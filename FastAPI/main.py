import numpy as np
import torch
import torch.nn as nn
import torch.nn.functional as F
import torch.optim as optim
from torch.utils.data import Dataset, DataLoader, TensorDataset, random_split
from torch.nn.utils.rnn import pad_sequence, pack_padded_sequence, pad_packed_sequence
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

from fastapi import FastAPI, HTTPException, Request, File, UploadFile
from fastapi.middleware.cors import CORSMiddleware
import shutil
from typing import List, Optional
from pydantic import BaseModel
import json
import time
import datetime
import os
import urllib.request

# =============================================================================
criterion = nn.CrossEntropyLoss()

tag_splitter = '@'
MAX_LEN = 20
LEARNING_RATE = 0.002
BATCH_SIZE = 8
EPOCHS = 15
HIDDEN_DIM = 128
EMB_DIM = 128
N_LAYERS = 2

PAD_TOKEN = '¶'
PAD_IDX = 0
UNK_TOKEN = '■'
UNK_IDX = 1
BOS_TOKEN = '#'
BOS_IDX = 2
EOS_TOKEN = '$'
EOS_IDX = 3

# =============================================================================
app = FastAPI()

@app.get("/")
async def root():
    return {"message": "Welcome to CommonMorph active learning API!"}

@app.get("/is_model_trained")
async def is_model_trained(langid: str):
  file_path = f'model/{langid}.model'
  if os.path.exists(file_path):
    creation_time = os.path.getctime(file_path)
    creation_date = datetime.datetime.fromtimestamp(creation_time)
    return {"message": creation_date.strftime("%Y-%m-%d %H:%M")}
  else:
    raise HTTPException(status_code=400, detail="No model trained for this language")

# =============================================================================  
# build the encoder and decoder
class VocabFromJson:
  def __init__(self, vocab_file):
    try:
      with open(vocab_file, 'r', encoding='utf-8') as f:
        data = json.load(f)
        self.tags_vocab_stoi = data['tag_vocab']
        self.char_vocab_stoi = data['char_vocab']
        self.tags_vocab_itos = {tag: idx for idx, tag in self.tags_vocab_stoi.items()}
        self.char_vocab_itos = {ch: idx for idx, ch in self.char_vocab_stoi.items()}
        
    except FileNotFoundError:
        raise FileNotFoundError(f"TSV file {vocab_file} not found")
    except ValueError:
        raise ValueError("TSV file must have two columns: character and ID")
  
  def encode_tagset(self, tagset):
    tagset = tagset.split(tag_splitter)
    return [self.tags_vocab_stoi.get(tag, UNK_IDX) for tag in tagset]

  def decode_tagset(self, indices):
    if not isinstance(indices, list):
      indices = [indices]
    return tag_splitter.join([self.tags_vocab_itos[idx] for idx in indices if (idx != PAD_IDX)])

  def encode_word(self, word, add_start_end=False):
    if add_start_end:
      word = BOS_TOKEN + word + EOS_TOKEN
    word = word.ljust(MAX_LEN, PAD_TOKEN)
    return [self.char_vocab_stoi.get(ch, UNK_IDX) for ch in word]

  def decode_word(self, chars):
    return ''.join([self.char_vocab_itos[idx] for idx in chars if (idx != PAD_IDX and idx != BOS_IDX and idx != EOS_IDX)])
      
  def decoder_confidence(self, indices, confidence):
    word = ''
    conf = []
    indices = indices.tolist()
    confidence = confidence.tolist()
    for i in range(len(indices)):
      if indices[i] != PAD_IDX and indices[i] != BOS_IDX and indices[i] != EOS_IDX:
        word += self.char_vocab_itos.get(indices[i], "")
        conf.append(confidence[i])
    return word, conf

# =============================================================================
class TestRequest(BaseModel):
  langid: str
  vocab_id: str
  test_file: str
@app.post("/testfile/")
def testfile(request: TestRequest):
  # read lines
  data = []
  with open(f"Test/{request.test_file}.tsv", "r", encoding="utf-8") as f:
    for line in f:
      parts = line.strip().split('\t')
      if len(parts) > 2:
        data.append(parts)
  
  # Load vocab file
  if request.vocab_id:
    vocab = VocabFromJson(f'model/{request.vocab_id}_vocab.json')
  else:
    vocab = VocabFromJson(f'model/{request.langid}_vocab.json')
  # Load TorchScript model
  model = torch.jit.load(f'model/{request.langid}.model')
  model.eval()
  count_correct = 0
  with open(f"Test/{request.test_file}_{request.langid}_results.tsv", "w", encoding="utf-8") as f:
    for i in range(len(data)):
      lemma, target, tagset = data[i]
      lemma = vocab.encode_word(lemma.strip())
      lemma = torch.tensor(lemma).unsqueeze(0)
      tagset = vocab.encode_tagset(tagset.strip())
      tagset = torch.tensor(tagset).unsqueeze(0)
      output = model(lemma, tagset)
      probs = F.softmax(output, dim=-1)
      confidence, predicted_class = torch.max(probs, dim=-1)
      predicted, conf = vocab.decoder_confidence(output.argmax(dim=2)[0], confidence.squeeze(0))
      conf_average = 0
      if len(conf) != 0:
        conf_average = round(sum(conf) / len(conf), 3)
      is_correct = 1 if (target == predicted) else 0
      count_correct += is_correct
      f.write(f"{is_correct}\t{target}\t{predicted}\t{conf_average}\n")
  return f'{count_correct} correct from {len(data)}'
  
# =============================================================================
class BatchSuggest(BaseModel):
  langid: str
  words: List[str]
  vocab_id: str

@app.post("/listpredict/")
def listpredict(request: BatchSuggest):
  if not request.words:
    raise HTTPException(status_code=400, detail="List is empty.")
  # Load vocab file
  if request.vocab_id:
    vocab = VocabFromJson(f'model/{request.vocab_id}_vocab.json')
  else:
    vocab = VocabFromJson(f'model/{request.langid}_vocab.json')
  # Load TorchScript model
  model = torch.jit.load(f'model/{request.langid}.model')
  model.eval()
  results = []
  for i in range(len(request.words)):
    lemma, tagset = request.words[i].split('_')
    lemma = vocab.encode_word(lemma.strip())
    lemma = torch.tensor(lemma).unsqueeze(0)
    tagset = vocab.encode_tagset(tagset.strip())
    tagset = torch.tensor(tagset).unsqueeze(0)
    output = model(lemma, tagset)
    probs = F.softmax(output, dim=-1)
    confidence, predicted_class = torch.max(probs, dim=-1)
    predicted, conf = vocab.decoder_confidence(output.argmax(dim=2)[0], confidence.squeeze(0))
    conf_average = round(sum(conf) / len(conf), 3)
    results.append({"pred": predicted, "conf": conf_average})
  return results

# =============================================================================
class SingleSuggest(BaseModel):
  langid: str
  input_data: str
  vocab_id: str
  
@app.post("/suggest")
def suggest(request: SingleSuggest):
  # Load vocab file
  if request.vocab_id:
    vocab = VocabFromJson(f'model/{request.vocab_id}_vocab.json')
  else:
    vocab = VocabFromJson(f'model/{request.langid}_vocab.json')
  # Load TorchScript model
  model = torch.jit.load(f'model/{request.langid}.model')
  model.eval()
  with torch.no_grad():
    lemma, tagset = request.input_data.split('_')
    lemma = vocab.encode_word(lemma.strip())
    lemma = torch.tensor(lemma)
    tagset = vocab.encode_tagset(tagset.strip())
    tagset = torch.tensor(tagset)
    output = model(lemma.unsqueeze(0), tagset.unsqueeze(0))
    probs = F.softmax(output, dim=-1)
    confidence, predicted_class = torch.max(probs, dim=-1)    
    predicted, conf = vocab.decoder_confidence(output.argmax(dim=2)[0], confidence.squeeze(0))
    conf_average = round(sum(conf) / len(conf), 3)
    conf = [round(i, 2) for i in conf]
  return {"predicted": predicted, "confidence": conf, "avg_confidence": conf_average}

# =============================================================================
def read_data(filename):
  with open(filename, 'r', encoding='utf-8') as f:
    data = []
    for line in f:
        parts = line.strip().split('\t')
        if len(parts) > 2:
          data.append([part.strip() for part in parts])
  return data

def extract(data, specials=None, splitter=None):
  stoi = {PAD_TOKEN: PAD_IDX, UNK_TOKEN: UNK_IDX}
  if specials is not None:
    for tok in specials:
      stoi[tok] = len(stoi)
  for line in data:
    if splitter is not None:
      for item in line.split(splitter):
        if item not in stoi:
          stoi[item] = len(stoi)
    else:
      for char in line:
        if char not in stoi:
          stoi[char] = len(stoi)
  return stoi
            
def ExtractVocabFromUniMorph(data, char_fields, tag_fields, specials=None, splitter=None): 
  char_data = []
  tag_data = []
  for line in data:
    for column in tag_fields:
      tag_data.append(line[column])
    for column in char_fields:
      char_data.append(line[column])
  char_stoi = extract(char_data, specials=[BOS_TOKEN, EOS_TOKEN])
  tags_stoi = extract(tag_data, splitter=tag_splitter)
  return char_stoi, tags_stoi

def encode_dataset(data, tag_fields, char_fields, vocab):
  encoded_dataset = []
  for row in data:
    for i in tag_fields + char_fields:
      if i==0: # lemma
        lemma_ids = torch.tensor(vocab.encode_word(row[i].strip()), dtype=torch.long)
      elif i==1:
        target_ids = torch.tensor(vocab.encode_word(row[i].strip(), add_start_end=True), dtype=torch.long)
      elif i==2:
        tag_ids = torch.tensor(vocab.encode_tagset(row[i].strip()), dtype=torch.long)
    encoded_dataset.append((lemma_ids, tag_ids, target_ids))
  return encoded_dataset

# =============================================================================
class LSTMModel(nn.Module):
  def __init__(self, char_vocab_size, tag_vocab_size, emb_dim, hidden_dim, n_layers, dropout=0.5):
    super(LSTMModel, self).__init__()
    self.hidden_dim = hidden_dim
    self.n_layers = n_layers
    self.bidirectional = False
    self.embedding_w = nn.Embedding(char_vocab_size, emb_dim, padding_idx=PAD_IDX)
    self.embedding_t = nn.Embedding(tag_vocab_size, emb_dim, padding_idx=PAD_IDX)
    self.lstm = nn.LSTM(emb_dim, hidden_dim, n_layers, batch_first=True, dropout=dropout if n_layers > 1 else 0)
    self.fc = nn.Linear(hidden_dim, char_vocab_size)
    self.dropout = nn.Dropout(dropout)

  def forward(self, src_words, src_tags):
    # embedding
    embedded_w = self.embedding_w(src_words) # [B, Len_w, emb_dim]
    embedded_t = self.embedding_t(src_tags) # [B, Len_w, emb_dim]
    if embedded_t.size(1) > 1:
      pad_size = embedded_w.size(1) - embedded_t.size(1)
      embedded_t = torch.cat([embedded_t, torch.zeros(embedded_t.size(0), pad_size, embedded_t.size(2))], dim=1)
    
    combined = embedded_w + embedded_t
    # LSTM
    lstm_out, (hidden, cell) = self.lstm(combined)
    output = self.fc(self.dropout(lstm_out)) 
    return output # [B, Len_w, emb_dim]

def train_LSTM(train_loader, epochs, model_name, word_vocab_size, tag_vocab_size, saved_state=None):    
  model = LSTMModel(word_vocab_size, tag_vocab_size, EMB_DIM, HIDDEN_DIM, N_LAYERS)
  # load the saved state for fine-tuning
  if saved_state is not None:
    model.load_state_dict(torch.load(saved_state))    
  # Train the model
  optimizer = optim.Adam(model.parameters(), lr=LEARNING_RATE)
  criterion = nn.CrossEntropyLoss(label_smoothing=0.1, ignore_index=PAD_IDX)
  start_time = time.time()
  train_loss_records = []
  for epoch in range(epochs):
    model.train()
    train_loss = 0.0
    for src_words, src_tags, target_word in train_loader:
      optimizer.zero_grad()
      output = model(src_words, src_tags)
      loss = criterion(output.view(-1, word_vocab_size),  target_word.view(-1))
      loss.backward()
      optimizer.step()
      train_loss += loss.item()
    train_loss /= len(train_loader)
    train_loss_records.append(train_loss)
  train_time = time.time() - start_time
  torch.save(model.state_dict(), f'model/{model_name}.pt')
  scripted_model = torch.jit.script(model)
  scripted_model.save(f'model/{model_name}.model')
  return train_loss_records, train_time

# =============================================================================
class TrainRequest(BaseModel):
  langid: str
  vocab_id: Optional[str] = None
  char_fields: Optional[List[int]] = None
  tag_fields: Optional[List[int]] = None

@app.post("/train")
def train(request: TrainRequest):
  # create folders if not exist
  if not os.path.exists(f'data'):
    os.makedirs(f'data')  
  if not os.path.exists(f'model'):
    os.makedirs(f'model')
  # default values for char_fields and tag_fields
  char_fields = request.char_fields or [0, 1]
  tag_fields = request.tag_fields or [2]
  # read the data from Common-Morph and write it to a local file
  url = f'https://common-morph.com/downlaod/unimorph/{request.langid}'
  response = urllib.request.urlopen(url)
  with open(f'data/{request.langid}.tsv', 'w', encoding='utf-8') as f:
    f.write(response.read().decode("utf-8"))
  data = read_data(f'data/{request.langid}.tsv')
  # build vocabs
  vocab_file = f'model/{request.langid}_vocab.json'
  if os.path.exists(vocab_file):
    old_vocab = VocabFromJson(f'model/{request.langid}_vocab.json')
  
  char_stoi, tags_stoi = ExtractVocabFromUniMorph(data, char_fields, tag_fields)
  # save vocabs to file
  with open(vocab_file, 'w') as f:
    json.dump({"char_vocab": char_stoi, "tag_vocab": tags_stoi}, f)
  # encode dataset
  new_vocab = VocabFromJson(vocab_file)    
  dataset = encode_dataset(data, tag_fields, char_fields, new_vocab)
  # get the dataloaders
  train_loader = DataLoader(dataset, batch_size=BATCH_SIZE, shuffle=False)
  # train the model
  word_vocab_size = len(new_vocab.char_vocab_stoi)
  tag_vocab_size = len(new_vocab.tags_vocab_stoi)
  # if model exists, finetune it
  if (os.path.exists(f'model/{request.langid}.pt')) and (old_vocab is not None) and (new_vocab == old_vocab):
    saved_state = f'model/{request.langid}.pt'
    train_loss_records, train_time = train_LSTM(train_loader, EPOCHS, request.langid, word_vocab_size, tag_vocab_size, saved_state=saved_state)
  else:
    train_loss_records, train_time = train_LSTM(train_loader, EPOCHS, request.langid, word_vocab_size, tag_vocab_size)
  return {"message": f"Model trained successfully in {train_time:.2f} seconds"}

# =============================================================================
class FinetuneRequest(BaseModel):
  langid: str
  finetune_id: str
  vocab_id: Optional[str] = None
  char_fields: Optional[List[int]] = None
  tag_fields: Optional[List[int]] = None

@app.post("/finetune")
def finetune(request: FinetuneRequest):
  # create folders if not exist
  if not os.path.exists(f'data'):
    os.makedirs(f'data')  
  if not os.path.exists(f'model'):
    os.makedirs(f'model')
  # default values for char_fields and tag_fields
  char_fields = request.char_fields or [0, 1]
  tag_fields = request.tag_fields or [2]
  # read the data from Common-Morph and write it to a local file
  url = f'https://common-morph.com/downlaod/unimorph/{request.finetune_id}'
  response = urllib.request.urlopen(url)
  with open(f'data/{request.finetune_id}.tsv', 'w', encoding='utf-8') as f:
    f.write(response.read().decode("utf-8"))  
  data = read_data(f'data/{request.finetune_id}.tsv')
  # build vocabs
  if request.vocab_id:
    vocab = VocabFromJson(f'model/{request.vocab_id}_vocab.json')
  else:    
    vocab = VocabFromJson(f'model/{request.langid}_vocab.json')
  # encode dataset
  # checking if new vocab items are in finetune file
  char_stoi, tags_stoi = ExtractVocabFromUniMorph(data, char_fields, tag_fields)
  if len(set(tags_stoi.keys()) - set(vocab.tags_vocab_stoi.keys())) > 0:
    raise HTTPException(status_code=400, detail="New tags detected! Re-train the model")
  if len(set(char_stoi.keys()) - set(vocab.char_vocab_stoi.keys())) > 0:
    raise HTTPException(status_code=400, detail="New characters detected! Re-train the model")
  
  dataset = encode_dataset(data, tag_fields, char_fields, vocab)
  # get the dataloaders
  train_loader = DataLoader(dataset, batch_size=BATCH_SIZE, shuffle=False)
  # train the model
  word_vocab_size = len(vocab.char_vocab_stoi)
  tag_vocab_size = len(vocab.tags_vocab_stoi)
  saved_state = f'model/{request.langid}.pt'
  train_loss_records, train_time = train_LSTM(train_loader, EPOCHS, request.finetune_id, word_vocab_size, tag_vocab_size, saved_state=saved_state)
  return {"message": f"Model trained successfully in {train_time:.2f} seconds"}