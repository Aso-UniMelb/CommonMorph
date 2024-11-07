let server = 'https://morphAPI.kurdinus.com/';
// let server = 'https://localhost:7242/';
let export_data = '';
// ========= Custom Sort
const customOrder = 'AaBbCcÇçDdEeÊêFfGgHhIiÎîJjKkLlMmNnOoPpQqRrSsŞşTtUuÛûVvWwXxYyZz';
const getCustomIndex = (char) => customOrder.indexOf(char);
function CustomSort(list) {
  list.sort((a, b) => {
    let textA = a.title.toLowerCase();
    let textB = b.title.toLowerCase();
    for (let i = 0; i < Math.min(textA.length, textB.length); i++) {
      const indexA = getCustomIndex(textA[i]);
      const indexB = getCustomIndex(textB[i]);
      if (indexA !== indexB) {
        return indexA - indexB;
      }
    }
    return textA.length - textB.length;
  });
  return list;
}
// ========= Dialects
let dialects = [];
function getDialects() {
  $('.loading').show();
  $('#detailsSlots').hide();
  $.ajax({
    url: server + 'Dialect/list',
    type: 'GET',
    success: function (data) {
      $('.loading').hide();
      dialects = data.sort((a, b) => a.title.localeCompare(b.title));
      var options = [];
      let html = '';
      for (let i = 0; i < dialects.length; i++) {
        options.push({ id: dialects[i].id, text: dialects[i].title });
        html += `<div class="dialect" onclick="selectDialect(${dialects[i].id})">
          <code>${dialects[i].code}</code> ${dialects[i].title}
          </div>`;
      }
      $('#lstDialects').html(html);
      $('#cmbDialects').select2({ data: options });
      $('#cmbFormsEntryDialect').select2({ data: options });
      $('#cmbFormsCheckDialect').select2({ data: options });
      getWordClasses(dialects[0].id);
      geLemmas(dialects[0].id);
      getFormsForEntry(dialects[0].id);
      getFormsForCheck(dialects[0].id);
    },
    error: function (data) {
      console.log(data);
    }
  });
}
$('#btnAddDialect').click(function () {
  $('#frmDialect').show();
  $('#txtDialectTitle').val('');
  $('#txtDialectCode').val('');
  $('#txtDialectDescription').val('');
  $('#txtDialectKeyboardLayout').val('');
  $('#DialectId').val('');
  $('#btnDialectSubmit').html('Add');
});
function selectDialect(id) {
  $('#btnDialectSubmit').html('Edit');
  $('#DialectId').val(id);
  $.ajax({
    url: server + 'Dialect/get',
    type: 'GET',
    data: {
      id: id
    },
    success: function (data) {
      $('#frmDialect').show();
      $('#txtDialectTitle').val(data.title);
      $('#txtDialectCode').val(data.code);
      $('#txtDialectDescription').val(data.description);
      $('#txtDialectKeyboardLayout').val(data.keyboardLayout);
    },
    error: function (data) {
      console.log(data);
    }
  });
}
function DialectSubmit() {
  if ($('#btnDialectSubmit').html() == 'Add') {
    insertDialect();
  } else {
    updateDialect();
  }
}
function insertDialect() {
  let title = $('#txtDialectTitle').val().trim();
  let code = $('#txtDialectCode').val().trim();
  let description = $('#txtDialectDescription').val().trim();
  let keyboardLayout = $('#txtDialectKeyboardLayout').val().trim();
  $.ajax({
    url: server + 'Dialect/insert',
    type: 'POST',
    data: {
      Title: title,
      Code: code,
      Description: description,
      KeyboardLayout: keyboardLayout
    },
    success: function (data) {
      $('#frmDialect').hide();
      getDialects();
    },
    error: function (data) {
      alert('Error');
    }
  });
}
function updateDialect() {
  let title = $('#txtDialectTitle').val().trim();
  let code = $('#txtDialectCode').val().trim();
  let description = $('#txtDialectDescription').val().trim();
  let keyboardLayout = $('#txtDialectKeyboardLayout').val().trim();
  $.ajax({
    url: server + 'Dialect/update',
    type: 'POST',
    data: {
      id: $('#DialectId').val(),
      title: title,
      code: code,
      description: description,
      keyboardLayout: keyboardLayout
    },
    success: function (data) {
      $('#frmDialect').hide();
      getDialects();
    },
    error: function (data) {
      alert('Error');
    }
  });
}
$('#btnDialectCancel').click(function () {
  $('#frmDialect').hide();
});
// ========= Word Classes
$(document).ready(function () {
  getDialects();
});
wordClasses = [];
$('#cmbDialects').on('select2:select', function (e) {
  var selectedItem = e.params.data;
  dialectId = selectedItem.id;
  $('#frmLemmas').hide();
  $('#cmbLemmaWordClass option').remove().trigger('change');
  getWordClasses(dialectId);
  geLemmas(dialectId);
});

function getWordClasses(DialectId) {
  $('#detailsSlots').hide();
  $('.loading').show();
  $.ajax({
    url: server + 'WordClass/list',
    type: 'GET',
    data: {
      DialectId: DialectId
    },
    success: function (data) {
      $('.loading').hide();
      wordClasses = data;
      var options = [];
      let html = '';
      for (let i = 0; i < wordClasses.length; i++) {
        options.push({ id: wordClasses[i].id, text: wordClasses[i].title });
        html += `<div class="wordClass">        
        ${wordClasses[i].title}
        <span class="smallButton" onclick="ShowSlots(${wordClasses[i].id}, '${wordClasses[i].title}')">Slots</span>
        <span class="smallButton" onclick="EditWordClass(${wordClasses[i].id})">✏️</span>
        </div>`;
      }
      $('#cmbLemmaWordClass').select2({ data: options });
      $('#lstWordClasses').html(html);
    },
    error: function (data) {
      console.log(data);
    }
  });
}
$('#btnAddWordClass').click(function () {
  $('#frmWordClass').show();
  $('#txtWordClassTitle').val('');
  $('#txtWordClassDescription').val('');
  $('#WordClassId').val('');
  $('#btnWordClassSubmit').html('Add');
});
function EditWordClass(id) {
  $('#btnWordClassSubmit').html('Edit');
  $('#detailsSlots').hide();
  $('#WordClassId').val(id);
  $.ajax({
    url: server + 'WordClass/get',
    type: 'GET',
    data: {
      id: id
    },
    success: function (data) {
      $('#frmWordClass').show();
      $('#txtWordClassTitle').val(data.title);
      $('#txtWordClassDescription').val(data.description);
    },
    error: function (data) {
      console.log(data);
    }
  });
}
function WordClassSubmit() {
  if ($('#btnWordClassSubmit').html() == 'Add') {
    insertWordClass();
  } else {
    updateWordClass();
  }
}
function insertWordClass() {
  let title = $('#txtWordClassTitle').val().trim();
  let description = $('#txtWordClassDescription').val().trim();
  let dialectId = $('#cmbDialects').val();
  $.ajax({
    url: server + 'WordClass/insert',
    type: 'POST',
    data: {
      Title: title,
      DialectID: dialectId,
      Description: description
    },
    success: function (data) {
      $('#frmWordClass').hide();
      getWordClasses(dialectId);
    },
    error: function (data) {
      alert('Error');
    }
  });
}
function updateWordClass() {
  let title = $('#txtWordClassTitle').val().trim();
  let description = $('#txtWordClassDescription').val().trim();
  let dialectId = $('#cmbDialects').val();
  $.ajax({
    url: server + 'WordClass/update',
    type: 'POST',
    data: {
      Id: $('#WordClassId').val(),
      Title: title,
      DialectID: dialectId,
      Description: description
    },
    success: function (data) {
      $('#frmWordClass').hide();
      getWordClasses(dialectId);
    },
    error: function (data) {
      alert('Error');
    }
  });
}
$('#btnWordClassCancel').click(function () {
  $('#frmWordClass').hide();
});
// ========= Lemmas
Lemmas = [];
function geLemmas(DialectId) {
  let d = dialects.findIndex(function (item, i) { return item.id == DialectId });
  validateInput('#txtLemmaEnglish', 'abcdefghijklmnopqrstuvwxyz');
  validateInput('#txtLemmaEntry', dialects[d].keyboardLayout);
  validateInput('#txtLemmaStem1', dialects[d].keyboardLayout);
  validateInput('#txtLemmaStem2', dialects[d].keyboardLayout);
  validateInput('#txtLemmaStem3', dialects[d].keyboardLayout);
  $('#detailsSlots').hide();
  $('#frmSlots').hide();
  $('.loading').show();
  $.ajax({
    url: server + 'Lemma/list',
    type: 'GET',
    data: {
      DialectId: DialectId
    },
    success: function (data) {
      $('.loading').hide();
      Lemmas = data.sort((a, b) => a.entry.localeCompare(b.entry));
      let html = '';
      for (let i = 0; i < Lemmas.length; i++) {
        let s1 = Lemmas[i].stem1;
        let s2 = Lemmas[i].stem2;
        let stems = s1 ? s1 : '';
        if (s2) {
          stems += '/' + s2;
        }
        html += `<div class="lemma" onclick="EditLemma(${i})">
        <span class="priority${Lemmas[i].priority}"></span>
        ${Lemmas[i].entry + ((stems) ? ' (' + stems + ')' : '')}
        <span class="LemmaWClass">${Lemmas[i].wClass}</span>
        </div>`;
      }
      $('#lstLemmas').html(html);
    },
    error: function (data) {
      console.log(data);
    }
  });
}
$('#btnAddLemma').click(function () {
  if (wordClasses.length == 0) {
    alert('No word classes! Please add word classes first.');
    return;
  }
  else {
    $('#frmLemmas').show();
    $('#frmLemmas').show();
    $('#txtLemmaEnglish').val('');
    $('#txtLemmaStem1').val('');
    $('#txtLemmaStem2').val('');
    $('#txtLemmaStem3').val('');
    $('#txtLemmaDescription').val('');
    $('#LemmaId').val('');
    $('#cmbLemmaPriority').val('1');
    $('#btnLemmasSubmit').html('Add');
  }
});
function LemmasSubmit() {
  if ($('#btnLemmasSubmit').html() == 'Add') {
    insertLemma();
  } else {
    updateLemma();
  }
}
function insertLemma() {
  let entry = $('#txtLemmaEntry').val().trim();
  let wordClassId = $('#cmbLemmaWordClass').val();
  let eng = $('#txtLemmaEnglish').val().trim();
  let stem1 = $('#txtLemmaStem1').val().trim();
  let stem2 = $('#txtLemmaStem2').val().trim();
  let stem3 = $('#txtLemmaStem3').val().trim();
  let priority = $('#cmbLemmaPriority').val();
  let description = $('#txtLemmaDescription').val().trim();
  let dialectId = $('#cmbDialects').val();
  $.ajax({
    url: server + 'Lemma/insert',
    type: 'POST',
    data: {
      Entry: entry,
      WordClassId: wordClassId,
      EngMeaning: eng,
      Stem1: stem1,
      Stem2: stem2,
      Stem3: stem3,
      Priority: priority,
      Description: description
    },
    success: function (data) {
      $('#frmLemmas').hide();
      geLemmas(dialectId);
    },
    error: function (data) {
      alert('Error');
    }
  });
}
function updateLemma() {
  let entry = $('#txtLemmaEntry').val().trim();
  let wordClassId = $('#cmbLemmaWordClass').val();
  let eng = $('#txtLemmaEnglish').val().trim();
  let stem1 = $('#txtLemmaStem1').val().trim();
  let stem2 = $('#txtLemmaStem2').val().trim();
  let stem3 = $('#txtLemmaStem3').val().trim();
  let description = $('#txtLemmaDescription').val().trim();
  let priority = $('#cmbLemmaPriority').val();
  let dialectId = $('#cmbDialects').val();
  $.ajax({
    url: server + 'Lemma/update',
    type: 'POST',
    data: {
      Id: $('#LemmaId').val(),
      Entry: entry,
      EngMeaning: eng,
      WordClassId: wordClassId,
      Stem1: stem1,
      Stem2: stem2,
      Stem3: stem3,
      Priority: priority,
      Description: description
    },
    success: function (data) {
      $('#frmLemmas').hide();
      geLemmas(dialectId);
    },
    error: function (data) {
      alert('Error');
    }
  });
}
$('#btnLemmasCancel').click(function () {
  $('#frmLemmas').hide();
});
function EditLemma(id) {
  $('#frmLemmas').show();
  $('#btnLemmasSubmit').html('Save');
  $('#txtLemmaEntry').val(Lemmas[id].entry);
  $('#txtLemmaEnglish').val(Lemmas[id].engMeaning);
  $('#txtLemmaStem1').val(Lemmas[id].stem1);
  $('#txtLemmaStem2').val(Lemmas[id].stem2);
  $('#txtLemmaStem3').val(Lemmas[id].stem3);
  $('#txtLemmaDescription').val(Lemmas[id].description);
  $('#LemmaId').val(Lemmas[id].id);
  $('#cmbLemmaWordClass').val(Lemmas[id].wordClassID).trigger('change');
  $('#cmbLemmaPriority').val(Lemmas[id].priority);
  $('#btnLemmasSubmit').html('Edit');
}
// ========= Slots
Slots = [];
function ShowSlots(WordClassID, WordClassTitle) {
  $('#detailsSlots').show();
  $('#lstSlots').html('');
  $('#frmSlots').hide();
  $('#frmWordClass').hide();
  $('#txtSlotWClass').html(WordClassTitle);
  $('#SlotWClassId').val(WordClassID);
  $('.loading').show();
  $.ajax({
    url: server + 'Slot/list',
    type: 'GET',
    data: {
      wordClassID: WordClassID
    },
    success: function (data) {
      $('.loading').hide();
      Slots = data.sort((a, b) => a.uniMorphTags.localeCompare(b.uniMorphTags));
      let html = '';
      for (let i = 0; i < Slots.length; i++) {
        html += `<div class="lemma" onclick="EditSlot(${i})">        
        <span class="priority${Slots[i].priority}"></span>
        ${Slots[i].uniMorphTags}</div>`;
      }
      $('#lstSlots').html(html);
    },
    error: function (data) {
      console.log(data);
    }
  });
}
$('#btnAddSlot').click(function () {
  $('#frmSlots').show();
  $('#txtSlotUniMorphTags').val('');
  $('#txtSlotFormula').val('');
  $('#SlotId').val('');
  $('#btnSlotsSubmit').html('Add');
});
$('#btnSlotsCancel').click(function () {
  $('#frmSlots').hide();
});
function SlotsSubmit() {
  if ($('#btnSlotsSubmit').html() == 'Add') {
    insertSlot();
  } else {
    updateSlot();
  }
}
function insertSlot() {
  let UniMorphTags = $('#txtSlotUniMorphTags').val().trim();
  UniMorphTags = UM_Sort(UniMorphTags);
  let Formula = $('#txtSlotFormula').val().trim();
  let WordClassId = $('#SlotWClassId').val();
  let priority = $('#cmbSlotPriority').val();
  $.ajax({
    url: server + 'Slot/insert',
    type: 'POST',
    data: {
      UniMorphTags: UniMorphTags,
      Formula: Formula,
      priority: priority,
      WordClassId: WordClassId
    },
    success: function (data) {
      $('#frmSlots').hide();
      ShowSlots(WordClassId);
    },
    error: function (data) {
      alert('Error');
      console.log(data);
    }
  });
}
function updateSlot() {
  let UniMorphTags = $('#txtSlotUniMorphTags').val().trim();
  UniMorphTags = UM_Sort(UniMorphTags);
  let Formula = $('#txtSlotFormula').val().trim();
  let WordClassId = $('#SlotWClassId').val();
  let priority = $('#cmbSlotPriority').val();
  $.ajax({
    url: server + 'Slot/update',
    type: 'POST',
    data: {
      id: $('#SlotId').val(),
      uniMorphTags: UniMorphTags,
      formula: Formula,
      priority: priority,
      wordClassID: WordClassId
    },
    success: function (data) {
      $('#frmSlots').hide();
      ShowSlots(WordClassId);
    },
    error: function (data) {
      alert('Error');
      console.log(data);
    }
  });
}
function EditSlot(id) {
  $('#frmSlots').show();
  $('#btnSlotsSubmit').html('Save');
  $('#txtSlotUniMorphTags').val(Slots[id].uniMorphTags);
  $('#txtSlotFormula').val(Slots[id].formula);
  $('#SlotId').val(Slots[id].id);
  $('#cmbSlotPriority').val(Slots[id].priority);
  $('#cmbSlotWordClass').val(Slots[id].wordClassID).trigger('change');
  $('#btnSlotsSubmit').html('Edit');
}
//=========== Speaker Part
Forms = [];

$('#cmbFormsEntryDialect').on('select2:select', function (e) {
  var selectedItem = e.params.data;
  dialectId = selectedItem.id;
  getFormsForEntry(dialectId);
});

$('#btnRefreshFormsEntry').click(function () {
  let dialectId = $('#cmbFormsEntryDialect').val();
  getFormsForEntry(dialectId);
});


$('#cmbFormsCheckDialect').on('select2:select', function (e) {
  var selectedItem = e.params.data;
  dialectId = selectedItem.id;
  getFormsForCheck(dialectId);
});

$('#btnRefreshFormsCheck').click(function () {
  let dialectId = $('#cmbFormsCheckDialect').val();
  getFormsForCheck(dialectId);
});


function getFormsForEntry(DialectId) {
  $('#tableFormsEntry').html('');
  $('.loading').show();
  $.ajax({
    url: server + 'Form/listForEntery',
    type: 'GET',
    data: {
      DialectId: DialectId
    },
    success: function (data) {
      $('.loading').hide();
      Forms = data;
      let d = dialects.findIndex(function (item, i) { return item.id == DialectId });
      let langCode = dialects[d].code;
      for (let i = 0; i < Forms.length; i++) {
        let w = Forms[i].formula;
        w = w.replace(/L/g, Forms[i].lemma);
        w = w.replace(/S1/g, Forms[i].S1);
        w = w.replace(/S2/g, Forms[i].S2);
        w = w.replace(/\+/g, '');
        $('#tableFormsEntry').append(`<div>
            <div class="LemmaFeatures">
              ${Forms[i].lemma}
              <span class="features">${UM_tag2word(Forms[i].tags, langCode)}</span>
            </div>
            <div class="formInputs">
              <input type="text" value="${w}" id="form${i}"/>
              <span class="formSubmit" onclick="getPrompt(UM_tags2prompt('${Forms[i].Eng}', '${Forms[i].tags}', '${langCode}'))">🤖</span>
              <span class="formSubmit" id="sForm${i}" onclick="SubmitForm(${i}, ${Forms[i].lemmaId}, ${Forms[i].slotId}, '${w}')">✅</span>
            </div>
          </div>`);
        let d = dialects.findIndex(function (item, i) { return item.id == DialectId });
        validateInput('#form' + i, dialects[d].keyboardLayout);
      }
    },
    error: function (data) {
      console.log(data);
    }
  });
}

export_data = '';
let shown_forms_checking = [];
function getFormsForCheck(DialectId) {
  $('#tableFormsCheck').html('');
  $('.loading').show();
  $.ajax({
    url: server + 'Form/listForCheck',
    type: 'GET',
    data: {
      DialectId: DialectId
    },
    success: function (data) {
      $('.loading').hide();
      Forms = data;
      let d = dialects.findIndex(function (item, i) { return item.id == DialectId });
      let langCode = dialects[d].code;
      export_data = '';
      for (let i = 0; i < Forms.length; i++) {
        let w = Forms[i].word;
        shown_forms_checking.push(Forms[i].id);
        export_data += `${Forms[i].lemma}\t${w}\t${Forms[i].tags}\n`;
        $('#tableFormsCheck').append(`<div>
          <div class="LemmaFeatures">
            ${Forms[i].lemma}
            <span class="features">${UM_tag2word(Forms[i].tags, langCode)}</span>
          </div>
          <div class="formInputs">
            <span class="addedForm">${w}</span>
            <span id="statYes${Forms[i].id}"></span>
            <span class="formSubmit" onclick="VoteYes(${Forms[i].id})">👍</span>
            <span class="formSubmit" onclick="VoteNo(${Forms[i].id})">👎</span>
            <span id="statNo${Forms[i].id}"></span>
            <span class="formSubmit" onclick="DeleteForm(${Forms[i].id})">❌</span>
          </div>
        </div>`);
        getVotes();
      }
    },
    error: function (data) {
      console.log(data);
    }
  });
}

function getVotes() {
  $.ajax({
    url: server + 'Form/getVotes',
    type: 'GET',
    data: {
      Ids: shown_forms_checking.join(',')
    },
    success: function (data) {
      for (let i = 0; i < data.length; i++) {
        let id = data[i].FormId;
        let type = data[i].type;
        let num = data[i].num;
        if (type == 0) {
          $('#statYes' + id).html(num);
        } else {
          $('#statNo' + id).html(num);
        }
      }
    },
    error: function (data) {
      console.log(data);
    }
  });
}

function SubmitForm(i, lemmaId, slotId, suggested) {
  let word = $('#form' + i).val().trim();
  $.ajax({
    url: server + 'Form/insert',
    type: 'POST',
    data: {
      LemmaId: lemmaId,
      SlotId: slotId,
      Suggested: suggested,
      Source: 1,
      Word: word
    },
    success: function (data) {
      $('#form' + i).prop('disabled', true);
      $('#form' + i).css("color", "green");
      $('#sForm' + i).hide();
    },
    error: function (data) {
      console.log(data);
    }
  });
}

function VoteNo(id) {
  $.ajax({
    url: server + 'Form/VoteNo',
    type: 'GET',
    data: {
      formId: id
    },
    success: function (data) {
      let dialectId = $('#cmbFormsCheckDialect').val();
      getFormsForCheck(dialectId);
    },
    error: function (data) {
      console.log(data);
    }
  });
}

function VoteYes(id) {
  $.ajax({
    url: server + 'Form/VoteYes',
    type: 'GET',
    data: {
      formId: id
    },
    success: function (data) {
      let dialectId = $('#cmbFormsCheckDialect').val();
      getFormsForCheck(dialectId);
    },
    error: function (data) {
      console.log(data);
    }
  });
}

function DeleteForm(id) {
  console.log(id);
  $.ajax({
    url: server + 'Form/delete',
    type: 'GET',
    data: {
      id: id
    },
    success: function (data) {
      let dialectId = $('#cmbFormsEntryDialect').val();
      getFormsForCheck(dialectId);
      getFormsForEntry(dialectId);
    },
    error: function (data) {
      console.log(data);
    }
  });
}

function getPrompt(prompt) {
  $('.loading').show();
  $.ajax({
    type: 'POST',
    url: 'https://api.groq.com/openai/v1/chat/completions',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer ' + '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'
    },
    data: JSON.stringify({
      'model': 'llama3-70b-8192',
      'messages': [{
        'role': 'user',
        'content': prompt
      }]
    }),
    success: function (data) {
      $('.loading').hide();
      alert(data.choices[0].message.content);
    },
    dataType: 'json'
  });
}

$('#btnExportData').click(function () {
  const textArea = document.createElement('textarea');
  textArea.value = export_data;
  document.body.appendChild(textArea);
  textArea.select();
  document.execCommand('copy');
  textArea.remove();
  alert('Inserted Data copied to clipboard');
});

function validateInput(selector, chars) {
  $(selector).off();
  $(selector).on('input', function () {
    var inputVal = $(this).val();
    var validChars = new RegExp(`^[ ${chars}]*$`);
    if (!validChars.test(inputVal)) {
      $(this).val(inputVal.replace(new RegExp(`[^ ${chars}]`, "g"), ''));
    }
  });
}