import os
import json
import time
import random
import datetime
import urllib.request
from typing import List, Optional, Tuple, Dict

import numpy as np
import torch
import torch.nn as nn
import torch.nn.functional as F
import torch.optim as optim
from torch.utils.data import Dataset, DataLoader
from pydantic import BaseModel
from fastapi import FastAPI, HTTPException

# Set CPU threads for optimal matrix operations
if torch.cuda.is_available():
    device = torch.device("cuda")
else:
    device = torch.device("cpu")
    torch.set_num_threads(max(1, os.cpu_count() or 4))

# =============================================================================
# Constants & Special Tokens
# =============================================================================
PAD_TOKEN = '<PAD>'
PAD_IDX = 0
UNK_TOKEN = '<UNK>'
UNK_IDX = 1
BOS_TOKEN = '<BOS>'
BOS_IDX = 2
EOS_TOKEN = '<EOS>'
EOS_IDX = 3

# Default Hyperparameters (Optimized for CPU)
EMB_DIM = 64
ENC_HID_DIM = 128
DEC_HID_DIM = 128
BATCH_SIZE = 32
EPOCHS = 20
LEARNING_RATE = 0.003
DROPOUT = 0.2
MAX_DECODE_LEN = 40

# =============================================================================
# Vocabulary
# =============================================================================
class MorphVocab:
    def __init__(self):
        self.stoi = {
            PAD_TOKEN: PAD_IDX,
            UNK_TOKEN: UNK_IDX,
            BOS_TOKEN: BOS_IDX,
            EOS_TOKEN: EOS_IDX,
        }
        self.itos = {v: k for k, v in self.stoi.items()}

    def build_vocab(self, data: List[Tuple[str, str, str]]):
        """
        Builds vocabulary from list of (lemma, target, tagset) tuples.
        """
        for lemma, target, tagset in data:
            # Process tag features
            tags = self.parse_tags(tagset)
            for tag in tags:
                tag_token = f"[{tag}]"
                if tag_token not in self.stoi:
                    idx = len(self.stoi)
                    self.stoi[tag_token] = idx
                    self.itos[idx] = tag_token

            # Process characters from lemma and target
            for ch in lemma.strip():
                if ch not in self.stoi:
                    idx = len(self.stoi)
                    self.stoi[ch] = idx
                    self.itos[idx] = ch

            for ch in target.strip():
                if ch not in self.stoi:
                    idx = len(self.stoi)
                    self.stoi[ch] = idx
                    self.itos[idx] = ch

    @staticmethod
    def parse_tags(tagset: str) -> List[str]:
        # Handle both ';' (UniMorph standard) and '@' delimiters
        delimiter = ';' if ';' in tagset else '@'
        return [t.strip() for t in tagset.split(delimiter) if t.strip()]

    def encode_src(self, lemma: str, tagset: str) -> List[int]:
        tags = self.parse_tags(tagset)
        tokens = [self.stoi.get(f"[{t}]", UNK_IDX) for t in tags]
        tokens.append(BOS_IDX)
        tokens.extend([self.stoi.get(ch, UNK_IDX) for ch in lemma.strip()])
        tokens.append(EOS_IDX)
        return tokens

    def encode_trg(self, target: str) -> List[int]:
        tokens = [BOS_IDX]
        tokens.extend([self.stoi.get(ch, UNK_IDX) for ch in target.strip()])
        tokens.append(EOS_IDX)
        return tokens

    def decode_tokens(self, indices: List[int]) -> str:
        chars = []
        for idx in indices:
            if idx in (PAD_IDX, BOS_IDX, EOS_IDX):
                continue
            token = self.itos.get(idx, "")
            if not token.startswith("["):  # skip tag tokens if any
                chars.append(token)
        return "".join(chars)

    def save(self, filepath: str):
        with open(filepath, 'w', encoding='utf-8') as f:
            json.dump({"stoi": self.stoi}, f, ensure_ascii=False, indent=2)

    @classmethod
    def load(cls, filepath: str) -> "MorphVocab":
        with open(filepath, 'r', encoding='utf-8') as f:
            data = json.load(f)
        vocab = cls()
        # Handle both new format {"stoi": ...} and legacy {"char_vocab": ..., "tag_vocab": ...}
        if "stoi" in data:
            vocab.stoi = data["stoi"]
        else:
            stoi = {PAD_TOKEN: PAD_IDX, UNK_TOKEN: UNK_IDX, BOS_TOKEN: BOS_IDX, EOS_TOKEN: EOS_IDX}
            for ch, _ in data.get("char_vocab", {}).items():
                if ch not in stoi:
                    stoi[ch] = len(stoi)
            for tag, _ in data.get("tag_vocab", {}).items():
                tag_token = f"[{tag}]"
                if tag_token not in stoi:
                    stoi[tag_token] = len(stoi)
            vocab.stoi = stoi
        vocab.itos = {v: k for k, v in vocab.stoi.items()}
        return vocab

    def __len__(self):
        return len(self.stoi)


# =============================================================================
# Dataset & Dynamic Batching Collate Function
# =============================================================================
class MorphDataset(Dataset):
    def __init__(self, data: List[Tuple[str, str, str]], vocab: MorphVocab):
        self.samples = []
        for lemma, target, tagset in data:
            src = vocab.encode_src(lemma, tagset)
            trg = vocab.encode_trg(target)
            self.samples.append((
                torch.tensor(src, dtype=torch.long),
                torch.tensor(trg, dtype=torch.long),
                lemma,
                target
            ))

    def __len__(self):
        return len(self.samples)

    def __getitem__(self, idx):
        return self.samples[idx]


def collate_fn(batch):
    src_list, trg_list, lemmas, targets = zip(*batch)
    src_padded = torch.nn.utils.rnn.pad_sequence(src_list, batch_first=True, padding_value=PAD_IDX)
    trg_padded = torch.nn.utils.rnn.pad_sequence(trg_list, batch_first=True, padding_value=PAD_IDX)
    return src_padded, trg_padded, lemmas, targets


# =============================================================================
# Neural Architecture: Attentional Seq2Seq
# =============================================================================
class BahdanauAttention(nn.Module):
    def __init__(self, enc_hid_dim: int, dec_hid_dim: int):
        super().__init__()
        self.attn = nn.Linear((enc_hid_dim * 2) + dec_hid_dim, dec_hid_dim)
        self.v = nn.Linear(dec_hid_dim, 1, bias=False)

    def forward(self, hidden, encoder_outputs, mask=None):
        # hidden: [batch_size, dec_hid_dim]
        # encoder_outputs: [batch_size, src_len, enc_hid_dim * 2]
        src_len = encoder_outputs.shape[1]
        hidden = hidden.unsqueeze(1).repeat(1, src_len, 1)
        energy = torch.tanh(self.attn(torch.cat((hidden, encoder_outputs), dim=2)))
        attention = self.v(energy).squeeze(2)  # [batch_size, src_len]

        if mask is not None:
            attention = attention.masked_fill(mask == 0, -1e10)

        return F.softmax(attention, dim=1)


class Seq2SeqMorph(nn.Module):
    def __init__(self, vocab_size: int, emb_dim: int = EMB_DIM, enc_hid: int = ENC_HID_DIM,
                 dec_hid: int = DEC_HID_DIM, pad_idx: int = PAD_IDX, dropout: float = 0.2):
        super().__init__()
        self.pad_idx = pad_idx
        self.vocab_size = vocab_size

        self.embedding = nn.Embedding(vocab_size, emb_dim, padding_idx=pad_idx)
        self.encoder = nn.GRU(emb_dim, enc_hid, batch_first=True, bidirectional=True)
        self.enc_to_dec = nn.Linear(enc_hid * 2, dec_hid)

        self.attention = BahdanauAttention(enc_hid, dec_hid)
        self.decoder = nn.GRU((enc_hid * 2) + emb_dim, dec_hid, batch_first=True)
        self.fc_out = nn.Linear(dec_hid + (enc_hid * 2) + emb_dim, vocab_size)
        self.dropout = nn.Dropout(dropout)

    def forward(self, src, trg, teacher_forcing_ratio: float = 0.5):
        # src: [B, src_len], trg: [B, trg_len]
        batch_size = src.shape[0]
        trg_len = trg.shape[1]

        enc_emb = self.dropout(self.embedding(src))
        enc_outputs, enc_hidden = self.encoder(enc_emb)

        # Initial decoder hidden state from bidirectional encoder
        dec_hidden = torch.tanh(self.enc_to_dec(torch.cat((enc_hidden[-2], enc_hidden[-1]), dim=1)))
        mask = (src != self.pad_idx)

        outputs = torch.zeros(batch_size, trg_len - 1, self.vocab_size, device=src.device)
        input_token = trg[:, 0]  # <BOS>

        for t in range(1, trg_len):
            input_emb = self.dropout(self.embedding(input_token))  # [B, emb_dim]
            a = self.attention(dec_hidden, enc_outputs, mask).unsqueeze(1)  # [B, 1, src_len]
            context = torch.bmm(a, enc_outputs).squeeze(1)  # [B, enc_hid * 2]

            dec_input = torch.cat((input_emb, context), dim=1).unsqueeze(1)
            dec_output, dec_hidden = self.decoder(dec_input, dec_hidden.unsqueeze(0))
            dec_hidden = dec_hidden.squeeze(0)

            pred = self.fc_out(torch.cat((dec_output.squeeze(1), context, input_emb), dim=1))
            outputs[:, t - 1] = pred

            teacher_force = random.random() < teacher_forcing_ratio
            top1 = pred.argmax(1)
            input_token = trg[:, t] if teacher_force else top1

        return outputs

    @torch.no_grad()
    def predict_sample(self, src_tokens: List[int], vocab: MorphVocab, max_len: int = MAX_DECODE_LEN):
        self.eval()
        src_tensor = torch.tensor(src_tokens, dtype=torch.long, device=device).unsqueeze(0)
        enc_emb = self.embedding(src_tensor)
        enc_outputs, enc_hidden = self.encoder(enc_emb)
        dec_hidden = torch.tanh(self.enc_to_dec(torch.cat((enc_hidden[-2], enc_hidden[-1]), dim=1)))
        mask = (src_tensor != self.pad_idx)

        input_token = torch.tensor([BOS_IDX], dtype=torch.long, device=device)
        pred_indices = []
        confidences = []

        for _ in range(max_len):
            input_emb = self.embedding(input_token)
            a = self.attention(dec_hidden, enc_outputs, mask).unsqueeze(1)
            context = torch.bmm(a, enc_outputs).squeeze(1)

            dec_input = torch.cat((input_emb, context), dim=1).unsqueeze(1)
            dec_output, dec_hidden = self.decoder(dec_input, dec_hidden.unsqueeze(0))
            dec_hidden = dec_hidden.squeeze(0)

            pred_logits = self.fc_out(torch.cat((dec_output.squeeze(1), context, input_emb), dim=1))
            probs = F.softmax(pred_logits, dim=-1)
            conf, pred_token = torch.max(probs, dim=-1)

            token_id = pred_token.item()
            if token_id == EOS_IDX:
                break

            pred_indices.append(token_id)
            confidences.append(round(conf.item(), 3))
            input_token = pred_token

        predicted_word = vocab.decode_tokens(pred_indices)
        avg_conf = round(sum(confidences) / len(confidences), 3) if confidences else 0.0
        return predicted_word, confidences, avg_conf


# =============================================================================
# Helper Utilities & Model Loading
# =============================================================================
def write_log(content: str):
    with open('log.txt', 'a', encoding='utf-8') as logger:
        now = datetime.datetime.now().strftime("%m/%d %H:%M:%S")
        logger.write(f"{now}\t{content}\n")


def read_tsv_data(filename: str) -> List[Tuple[str, str, str]]:
    data = []
    with open(filename, 'r', encoding='utf-8') as f:
        for line in f:
            parts = line.strip().split('\t')
            if len(parts) >= 3:
                data.append((parts[0].strip(), parts[1].strip(), parts[2].strip()))
    return data


def split_data(data: List[Tuple[str, str, str]], train_ratio=0.8, dev_ratio=0.1, seed=42):
    random.seed(seed)
    shuffled = data.copy()
    random.shuffle(shuffled)
    n = len(shuffled)
    n_train = int(n * train_ratio)
    n_dev = int(n * dev_ratio)
    train_data = shuffled[:n_train]
    dev_data = shuffled[n_train:n_train + n_dev]
    test_data = shuffled[n_train + n_dev:]
    return train_data, dev_data, test_data


def load_model_and_vocab(langid: str, vocab_id: Optional[str] = None):
    v_id = vocab_id if vocab_id else langid
    vocab_path = f"model/{v_id}_vocab.json"
    model_path = f"model/{langid}.pt"

    if not os.path.exists(vocab_path):
        raise HTTPException(status_code=404, detail=f"Vocab file '{vocab_path}' not found")
    if not os.path.exists(model_path):
        raise HTTPException(status_code=404, detail=f"Model weights '{model_path}' not found")

    vocab = MorphVocab.load(vocab_path)
    model = Seq2SeqMorph(vocab_size=len(vocab), emb_dim=EMB_DIM, enc_hid=ENC_HID_DIM, dec_hid=DEC_HID_DIM, pad_idx=PAD_IDX)
    model.load_state_dict(torch.load(model_path, map_location=device))
    model.to(device)
    model.eval()
    return model, vocab


# =============================================================================
# Training Engine
# =============================================================================
def train_morph_model(data: List[Tuple[str, str, str]], model_name: str, epochs: int = EPOCHS,
                      batch_size: int = BATCH_SIZE, lr: float = LEARNING_RATE,
                      saved_state: Optional[str] = None) -> Dict:
    os.makedirs('model', exist_ok=True)
    start_time = time.time()

    # 1. 80 / 10 / 10 Data Split
    train_data, dev_data, test_data = split_data(data, 0.8, 0.1, seed=42)

    # 2. Build Vocabulary
    vocab = MorphVocab()
    vocab.build_vocab(data)
    vocab_file = f"model/{model_name}_vocab.json"
    vocab.save(vocab_file)

    train_dataset = MorphDataset(train_data, vocab)
    dev_dataset = MorphDataset(dev_data, vocab)
    test_dataset = MorphDataset(test_data, vocab)

    train_loader = DataLoader(train_dataset, batch_size=batch_size, shuffle=True, collate_fn=collate_fn)
    dev_loader = DataLoader(dev_dataset, batch_size=batch_size, shuffle=False, collate_fn=collate_fn)
    test_loader = DataLoader(test_dataset, batch_size=batch_size, shuffle=False, collate_fn=collate_fn)

    # 3. Instantiate Model
    model = Seq2SeqMorph(vocab_size=len(vocab), emb_dim=EMB_DIM, enc_hid=ENC_HID_DIM, dec_hid=DEC_HID_DIM, pad_idx=PAD_IDX)
    if saved_state and os.path.exists(saved_state):
        try:
            model.load_state_dict(torch.load(saved_state, map_location=device))
        except Exception as e:
            write_log(f"Warning: could not load saved state: {e}")

    model.to(device)
    optimizer = optim.AdamW(model.parameters(), lr=lr, weight_decay=1e-4)
    scheduler = optim.lr_scheduler.CosineAnnealingLR(optimizer, T_max=epochs)
    criterion = nn.CrossEntropyLoss(ignore_index=PAD_IDX, label_smoothing=0.05)

    best_dev_acc = -1.0
    best_model_path = f"model/{model_name}.pt"

    for epoch in range(1, epochs + 1):
        model.train()
        train_loss = 0.0
        # Linear decay of teacher forcing
        tf_ratio = max(0.3, 0.8 - (0.5 * (epoch / epochs)))

        for src, trg, _, _ in train_loader:
            src, trg = src.to(device), trg.to(device)
            optimizer.zero_grad()
            output = model(src, trg, teacher_forcing_ratio=tf_ratio)
            # trg[:, 1:] excludes <BOS>
            loss = criterion(output.reshape(-1, len(vocab)), trg[:, 1:].reshape(-1))
            loss.backward()
            torch.nn.utils.clip_grad_norm_(model.parameters(), max_norm=1.0)
            optimizer.step()
            train_loss += loss.item()

        scheduler.step()
        train_loss /= len(train_loader)

        # Evaluate on Dev set (Exact Match)
        model.eval()
        correct = 0
        total = len(dev_data)
        for lemma, target, tagset in dev_data:
            src_tokens = vocab.encode_src(lemma, tagset)
            pred_word, _, _ = model.predict_sample(src_tokens, vocab)
            if pred_word == target:
                correct += 1
        dev_acc = (correct / total) * 100

        write_log(f"Epoch {epoch}/{epochs} | Loss: {train_loss:.4f} | Dev Acc: {dev_acc:.2f}%")

        if dev_acc > best_dev_acc:
            best_dev_acc = dev_acc
            torch.save(model.state_dict(), best_model_path)

    # 4. Final Evaluation on Test Set with Best Model
    model.load_state_dict(torch.load(best_model_path, map_location=device))
    model.eval()
    test_correct = 0
    for lemma, target, tagset in test_data:
        src_tokens = vocab.encode_src(lemma, tagset)
        pred_word, _, _ = model.predict_sample(src_tokens, vocab)
        if pred_word == target:
            test_correct += 1
    test_acc = (test_correct / len(test_data)) * 100

    elapsed = time.time() - start_time
    write_log(f"Finished {model_name} in {elapsed:.2f}s | Best Dev: {best_dev_acc:.2f}% | Test: {test_acc:.2f}%")

    return {
        "train_time_seconds": round(elapsed, 2),
        "dev_accuracy_percent": round(best_dev_acc, 2),
        "test_accuracy_percent": round(test_acc, 2),
        "total_samples": len(data),
        "train_samples": len(train_data),
        "dev_samples": len(dev_data),
        "test_samples": len(test_data),
        "vocab_size": len(vocab)
    }


# =============================================================================
# FastAPI Application & Endpoints
# =============================================================================
app = FastAPI(title="CommonMorph Neural Inflection API")

@app.get("/")
async def root():
    return {"message": "Welcome to CommonMorph Neural Inflection API!"}


@app.get("/is_model_trained")
async def is_model_trained(langid: str):
    file_path = f'model/{langid}.pt'
    if os.path.exists(file_path):
        creation_time = os.path.getctime(file_path)
        creation_date = datetime.datetime.fromtimestamp(creation_time)
        return {"message": creation_date.strftime("%Y-%m-%d %H:%M"), "model": langid}
    else:
        raise HTTPException(status_code=400, detail=f"No model trained for language '{langid}'")


# -----------------------------------------------------------------------------
class TrainRequest(BaseModel):
    langid: str
    epochs: Optional[int] = EPOCHS
    batch_size: Optional[int] = BATCH_SIZE
    lr: Optional[float] = LEARNING_RATE


@app.post("/train")
def train_endpoint(request: TrainRequest):
    os.makedirs('data', exist_ok=True)
    local_tsv = f'data/{request.langid}.tsv'

    # Download fresh UniMorph data from backend (prioritize local backend, fallback to remote)
    backend_url = os.environ.get("BACKEND_URL", "http://localhost:5041")
    url = f'{backend_url}/download/unimorph/{request.langid}'
    try:
        write_log(f'Fetching latest data for {request.langid} from {url}')
        with urllib.request.urlopen(url, timeout=10) as response:
            content = response.read().decode("utf-8")
            if len(content.strip()) > 0:
                with open(local_tsv, 'w', encoding='utf-8') as f:
                    f.write(content)
    except Exception as e:
        write_log(f'Local backend fetch failed ({e}), checking fallback or existing file')
        if not os.path.exists(local_tsv):
            try:
                url_fallback = f'https://common-morph.com/download/unimorph/{request.langid}'
                with urllib.request.urlopen(url_fallback, timeout=15) as response:
                    content = response.read().decode("utf-8")
                    with open(local_tsv, 'w', encoding='utf-8') as f:
                        f.write(content)
            except Exception as ex:
                raise HTTPException(status_code=400, detail=f"Failed to fetch data for language {request.langid}: {ex}")

    data = read_tsv_data(local_tsv)
    if not data:
        raise HTTPException(status_code=400, detail=f"Dataset for language {request.langid} is empty or not formatted correctly")

    target_epochs = request.epochs if (request.epochs is not None and request.epochs > 0) else EPOCHS
    results = train_morph_model(
        data=data,
        model_name=request.langid,
        epochs=target_epochs,
        batch_size=request.batch_size or BATCH_SIZE,
        lr=request.lr or LEARNING_RATE
    )
    return {
        "message": f"Model {request.langid} trained successfully in {results['train_time_seconds']} seconds",
        "metrics": results
    }


# -----------------------------------------------------------------------------
class SingleSuggest(BaseModel):
    langid: str
    input_data: str
    vocab_id: Optional[str] = None


@app.post("/suggest")
def suggest(request: SingleSuggest):
    model, vocab = load_model_and_vocab(request.langid, request.vocab_id)
    if '_' not in request.input_data:
        raise HTTPException(status_code=400, detail="input_data must be formatted as 'lemma_tagset'")

    lemma, tagset = request.input_data.split('_', 1)
    src_tokens = vocab.encode_src(lemma.strip(), tagset.strip())
    predicted, conf, conf_average = model.predict_sample(src_tokens, vocab)

    return {
        "predicted": predicted,
        "confidence": conf,
        "avg_confidence": conf_average
    }


# -----------------------------------------------------------------------------
class BatchSuggest(BaseModel):
    langid: str
    words: List[str]
    vocab_id: Optional[str] = None


@app.post("/listpredict/")
def listpredict(request: BatchSuggest):
    if not request.words:
        raise HTTPException(status_code=400, detail="List is empty.")

    model, vocab = load_model_and_vocab(request.langid, request.vocab_id)
    results = []

    for item in request.words:
        if '_' not in item:
            continue
        lemma, tagset = item.split('_', 1)
        src_tokens = vocab.encode_src(lemma.strip(), tagset.strip())
        predicted, conf, conf_average = model.predict_sample(src_tokens, vocab)
        results.append({"pred": predicted, "conf": conf_average})

    return results


# -----------------------------------------------------------------------------
class TestRequest(BaseModel):
    langid: str
    vocab_id: Optional[str] = None
    test_file: str


@app.post("/testfile/")
def testfile(request: TestRequest):
    os.makedirs('Test', exist_ok=True)
    test_path = f"Test/{request.test_file}.tsv"
    if not os.path.exists(test_path):
        test_path = f"data/{request.test_file}.tsv"
        if not os.path.exists(test_path):
            raise HTTPException(status_code=404, detail=f"Test file '{request.test_file}' not found in Test/ or data/")

    data = read_tsv_data(test_path)
    model, vocab = load_model_and_vocab(request.langid, request.vocab_id)

    count_correct = 0
    out_path = f"Test/{request.test_file}_{request.langid}_results.tsv"
    with open(out_path, "w", encoding="utf-8") as f:
        for lemma, target, tagset in data:
            src_tokens = vocab.encode_src(lemma, tagset)
            predicted, conf, avg_conf = model.predict_sample(src_tokens, vocab)
            is_correct = 1 if (target == predicted) else 0
            count_correct += is_correct
            f.write(f"{is_correct}\t{target}\t{predicted}\t{avg_conf}\n")

    accuracy = round((count_correct / len(data)) * 100, 2) if data else 0
    return {
        "result": f"{count_correct} correct from {len(data)}",
        "accuracy_percent": accuracy,
        "output_file": out_path
    }


# -----------------------------------------------------------------------------
class FinetuneRequest(BaseModel):
    langid: str
    finetune_id: str
    vocab_id: Optional[str] = None
    epochs: Optional[int] = 10


@app.post("/finetune")
def finetune(request: FinetuneRequest):
    local_tsv = f'data/{request.finetune_id}.tsv'
    if not os.path.exists(local_tsv):
        url = f'https://common-morph.com/download/unimorph/{request.finetune_id}'
        with urllib.request.urlopen(url) as response:
            with open(local_tsv, 'w', encoding='utf-8') as f:
                f.write(response.read().decode("utf-8"))

    data = read_tsv_data(local_tsv)
    saved_state = f'model/{request.langid}.pt'
    results = train_morph_model(
        data=data,
        model_name=request.finetune_id,
        epochs=request.epochs or 10,
        saved_state=saved_state if os.path.exists(saved_state) else None
    )
    return {"message": f"Model fine-tuned successfully in {results['train_time_seconds']}s", "metrics": results}