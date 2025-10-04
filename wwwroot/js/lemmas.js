Lemmas = [];
function getLemmas(id) {
  validateInput('#txtLemmaEntry', myLang.validchars);
  validateInput('#txtLemmaStem1', myLang.validchars);
  validateInput('#txtLemmaStem2', myLang.validchars);
  validateInput('#txtLemmaStem3', myLang.validchars);
  validateInput('#txtLemmaStem4', myLang.validchars);
  $('#detailsSlots').hide();
  $('#frmSlots').hide();
  $('.loading').show();

  $.ajax({
    url: '/Lemma/list',
    type: 'GET',
    data: {
      LangId: id,
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
        html += `<li class="lemma" onclick="EditLemma(${i})">
        <span class="priority${Lemmas[i].priority}"></span>
        ${Lemmas[i].entry + (stems ? ' (' + stems + ')' : '')}
        <span class="LemmaWClass">${Lemmas[i].wClass}</span>
        </li>`;
      }
      $('#lstLemmas').html(html);
    },
    error: function (data) {
      console.log(data);
    },
  });
}

$('#btnAddLemma').click(function () {
  if (ParadigmClasses.length == 0) {
    alert('No word classes! Please add word classes first.');
    return;
  } else {
    $('#frmLemmas').show();
    $('#txtLemmaEntry').val('');
    $('#txtLemmaEnglish').val('');
    $('#txtLemmaStem1').val('');
    $('#txtLemmaStem2').val('');
    $('#txtLemmaStem3').val('');
    $('#txtLemmaStem4').val('');
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
  $.ajax({
    url: '/Lemma/insert',
    type: 'POST',
    data: {
      Entry: $('#txtLemmaEntry').val().trim(),
      ParadigmClassId: $('#cmbLemmaParadigmClass').val(),
      EngMeaning: $('#txtLemmaEnglish').val().trim(),
      Stem1: $('#txtLemmaStem1').val().trim(),
      Stem2: $('#txtLemmaStem2').val().trim(),
      Stem3: $('#txtLemmaStem3').val().trim(),
      Stem4: $('#txtLemmaStem4').val().trim(),
      Priority: $('#cmbLemmaPriority').val(),
      Description: $('#txtLemmaDescription').val().trim(),
    },
    success: function (data) {
      $('#frmLemmas').hide();
      getLemmas(myLang.id);
    },
    error: function (data) {
      alert('Error');
    },
  });
}

function updateLemma() {
  $.ajax({
    url: '/Lemma/update',
    type: 'POST',
    data: {
      Id: $('#LemmaId').val(),
      Entry: $('#txtLemmaEntry').val().trim(),
      EngMeaning: $('#txtLemmaEnglish').val().trim(),
      ParadigmClassId: $('#cmbLemmaParadigmClass').val(),
      Stem1: $('#txtLemmaStem1').val().trim(),
      Stem2: $('#txtLemmaStem2').val().trim(),
      Stem3: $('#txtLemmaStem3').val().trim(),
      Stem4: $('#txtLemmaStem4').val().trim(),
      Priority: $('#cmbLemmaPriority').val(),
      Description: $('#txtLemmaDescription').val().trim(),
    },
    success: function (data) {
      $('#frmLemmas').hide();
      getLemmas(myLang.id);
    },
    error: function (data) {
      alert('Error');
    },
  });
}

$('#btnLemmasCancel').click(function () {
  $('#frmLemmas').hide();
});

function EditLemma(id) {
  $('#frmLemmas').show();
  $('#btnLemmasSubmit').html('Save');
  $('#txtLemmaEntry').val(Lemmas[id].entry);
  $('#txtLemmaEnglish').val(Lemmas[id].engmeaning);
  $('#txtLemmaStem1').val(Lemmas[id].stem1);
  $('#txtLemmaStem2').val(Lemmas[id].stem2);
  $('#txtLemmaStem3').val(Lemmas[id].stem3);
  $('#txtLemmaStem4').val(Lemmas[id].stem4);
  $('#txtLemmaDescription').val(Lemmas[id].description);
  $('#LemmaId').val(Lemmas[id].id);
  $('#cmbLemmaParadigmClass').val(Lemmas[id].paradigmclassid).trigger('change');
  $('#cmbLemmaPriority').val(Lemmas[id].priority);
  $('#btnLemmasSubmit').html('Save');
}

// ========= import Lemmas
// show import form
$('#btnImportLemmas').click(function () {
  $('#frmImportLemmas').show();
});
// cancel import form
$('#btnLemmaImportCancel').click(function () {
  $('#frmImportLemmas').hide();
});
// submit import form
function LemmaImportSubmit() {
  $('.loading').show();
  $.ajax({
    url: '/Lemma/import',
    type: 'POST',
    data: {
      file: $('#txtLemmaImport').val(),
      langid: myLang.id,
    },
    success: function (data) {
      $('.loading').hide();
      $('#frmImportLemmas').hide();
      getLemmas(myLang.id);
    },
    error: function (data) {
      alert('Error');
    },
  });
}

// export lemmas
$('#btnExportLemmas').click(function () {
  let file = Lemmas.map((lemma) => {
    const stems = [lemma.stem1, lemma.stem2, lemma.stem3, lemma.stem4].filter(
      Boolean
    );
    return [lemma.wClass, lemma.entry, lemma.engmeaning, ...stems].join('\t');
  }).join('\n');
  var blob = new Blob([file], { type: 'text/plain' });
  var link = document.createElement('a');
  link.href = URL.createObjectURL(blob);
  link.download = myLang.title + '_Lemmas.tsv';
  link.click();
});

$(document).ready(function () {
  $('#frmLemmas').hide();
  $('#cmbLemmaParadigmClass option').remove().trigger('change');
  getLemmas(myLang.id);
});
