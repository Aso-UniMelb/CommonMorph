Lemmas = [];
function getLemmas(id) {
  validateInput('#txtLemmaEntry', myLang.validchars);
  validateInput('#txtLemmaStem1', myLang.validchars);
  validateInput('#txtLemmaStem2', myLang.validchars);
  validateInput('#txtLemmaStem3', myLang.validchars);
  validateInput('#txtLemmaStem4', myLang.validchars);
  $('#detailsStructures').hide();
  $('#frmStructures').hide();
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
        ${Lemmas[i].entry}
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
  if (InflectionClasses.length == 0) {
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
    $('#txtLemmaUniMorphTags').val('');
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
      InflectionClassId: $('#cmbLemmaInflectionClass').val(),
      EngMeaning: $('#txtLemmaEnglish').val().trim(),
      Stem1: $('#txtLemmaStem1').val().trim(),
      Stem2: $('#txtLemmaStem2').val().trim(),
      Stem3: $('#txtLemmaStem3').val().trim(),
      Stem4: $('#txtLemmaStem4').val().trim(),
      UniMorphTags: $('#txtLemmaUniMorphTags').val().trim(),
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
      InflectionClassId: $('#cmbLemmaInflectionClass').val(),
      Stem1: $('#txtLemmaStem1').val().trim(),
      Stem2: $('#txtLemmaStem2').val().trim(),
      Stem3: $('#txtLemmaStem3').val().trim(),
      Stem4: $('#txtLemmaStem4').val().trim(),
      UniMorphTags: $('#txtLemmaUniMorphTags').val().trim(),
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
  $('#txtLemmaUniMorphTags').val(Lemmas[id].unimorphtags);
  if (Lemmas[id].unimorphtags) {
    updateLemmaUMselects(Lemmas[id].unimorphtags.split(/[;+]/));
  } else {
    updateLemmaUMselects([]);
  }
  $('#txtLemmaDescription').val(Lemmas[id].description);
  $('#LemmaId').val(Lemmas[id].id);
  $('#cmbLemmaInflectionClass').val(Lemmas[id].inflectionclassid).trigger('change');
  $('#cmbLemmaPriority').val(Lemmas[id].priority);
  $('#btnLemmasSubmit').html('Save');
}


// UMtagSelector
$('#LemmaUMtagSelector').append(
  `<div class="UMtagSelector"><div><i>Add feature:</i></div>
    <label>Dimension:</label>
    <select id="dimensionL"></select>
    <br />
    <label>Feature:</label>
    <select id="featureL"></select>
    <button type="button" id="btnAddUMtagL">⇧ Add</button>
  </div>
  
  `
);
AggrementDimensions.forEach((dim) => {
  $('#dimensionL').append(`<option value="${dim}">${dim}</option>`);
});

$('#dimensionL').change(function () {
  $('#featureL').html(' ');
  UM.forEach((tag) => {
    let dim = tag.d;
    if (dim == $('#dimensionL').val()) {
      let feat = tag.f;
      $('#featureL').append(
        `<option value="${tag.l}" title="${feat}">${feat}</option>`
      );
    }
  });
});
$('#dimensionL').change();

$('#btnAddUMtagL').click(function () {
  let dim = $('#dimensionL').val();
  let feat = $('#featureL').val();
  let feat_title = $('#featureL  option:selected').attr('title');
  if ($('#F_' + feat).length == 0 && dim && feat) {
    $('#addedUMtagsL').append(
      `<div class="UMtag" id="F_${feat}"><span class="remove" onclick="RemoveFeatL('${feat}')">&times;</span> ${dim}: ${feat_title}</div>`
    );
  }
  UpdateUniMorphTagSetL();
});

function RemoveFeatL(feat) {
  $('#F_' + feat).remove();
  UpdateUniMorphTagSetL();
}

function UpdateUniMorphTagSetL() {
  tagset = [];
  let added = $('#addedUMtagsL').children();
  console.log(added);
  for (let i = 0; i < added.length; i++) {
    console.log(added[i]);
    let feat = $(added[i]).attr('id').replace('F_', '');
    tagset.push(feat);
  }
  $('#txtLemmaUniMorphTags').val(UM_Sort(tagset.join(';')));
}

function updateLemmaUMselects(tagset) {
  $('#addedUMtagsL').html('');
  tagset.forEach((feat) => {
    if (feat) {
      let vec = UM.find((item) => item.l === feat);
      $('#addedUMtagsL').append(
        `<div class="UMtag" id="F_${feat}"><span class="remove" onclick="RemoveFeatL('${feat}')">&times;</span> ${vec.d}: ${vec.f}</div>`
      );
    }
  });
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

// export lexicon
$('#btnExportLemmas').click(function () {
  // temporarily sort lemmas by wClass, then by entry
  let sortedLemmas = [...Lemmas].sort((a, b) => {
    if (a.wClass < b.wClass) return -1;
    if (a.wClass > b.wClass) return 1;
    if (a.entry < b.entry) return -1;
    if (a.entry > b.entry) return 1;
    return 0;
  });
  let file = sortedLemmas.map((lemma) => {
    const stems = [lemma.stem1, lemma.stem2, lemma.stem3, lemma.stem4].filter(
      Boolean
    );
    // replace commas in meaning with semicolon
    let engmeaning = lemma.engmeaning.replace(/,/g, ";");
    return [lemma.wClass, lemma.entry, engmeaning, lemma.unimorphtags, ...stems].join(',');
  }).join('\n');
  var blob = new Blob([file], { type: 'text/plain' });
  var link = document.createElement('a');
  link.href = URL.createObjectURL(blob);
  link.download = myLang.title + '_export_lexicon.csv';
  link.click();
});

$(document).ready(function () {
  $('#frmLemmas').hide();
  $('#cmbLemmaInflectionClass option').remove().trigger('change');
  getLemmas(myLang.id);
});
