let lemmaPage = 0;
let slotPage = 0;
let nnPage = 0;
let dir = 'ltr';
let rules = [];

function getRules(langid) {
  $('.loading').show();
  $.ajax({
    url: '/Morphophonology/list',
    type: 'GET',
    data: {
      LangId: langid,
    },
    success: function (data) {
      $('.loading').hide();
      rules = data;
      let html = '<option value="0"></option>';
      for (let i = 0; i < rules.length; i++) {
        html += `<option value="${rules[i].id}">${rules[i].title}</option>`;
      }
      $('#cmbMorphophonemicRule').html(html);
    },
    error: function (data) {
      console.log(data);
    },
  });
}

function getLangStats(langid) {
  $('.loading').show();
  $('#langstats').html('');
  $.ajax({
    url: '/Elicit/GetStats',
    type: 'GET',
    data: {
      langid: langid,
    },
    success: function (data) {
      $('.loading').hide();
      if (data.countAll == 0) {
        $('#langstats').append(`<span>No linguistic data added!</div>`);
      } else {
        let progress = Math.ceil(
          ((data.countAll - data.remaining) * 100) / data.countAll
        );
        $('#langstats').append(
          `<div style="width:${progress}%"></div><span>${
            data.countAll - data.remaining
          } from ${data.countAll} (${progress}%)</span>`
        );
      }
    },
    error: function (data) {
      console.log(data);
    },
  });
}

function EntryGetTable(myLangId, pg, type) {
  getLangStats(myLangId);
  $('.loading').show();
  $('#dataentry').html('');
  $.ajax({
    url: '/Elicit/EntryGetTableBy' + type,
    type: 'GET',
    data: {
      langid: myLangId,
      page: pg,
    },
    success: function (data) {
      if (data.length == 0 || data.pool.length == 0) {
        $('#dataentry').html(
          `<div class="done">${i18n[myMetalang]['elicit_end']}</div>`
        );
        $('.loading').hide();
        return;
      }
      $('#findReplace').show();
      let pool = data.pool;

      if (type == 'Slot') {
        let slot = data.slot;
        $('#dataentry').append(
          `<h2>${slot.title}</h2>
          <form id="dataForm" dir="${dir}"><div id="formFields"></div></form>`
        );
        for (let i = 0; i < pool.length; i++) {
          let C = pool[i];
          let w = WordFromFormula(
            slot.formula,
            C.lemma,
            C.stem1,
            C.stem2,
            C.stem3,
            C.stem4,
            C.a
          );
          let title = slot.title;
          if (C.atitle) {
            title += `| ${C.atitle}`;
          }
          title += ' (' + C.lemma + ')';
          let ids = `${C.lemmaid},${slot.id},${C.agreementid ?? 0}`;
          let hint = myLevel == '1' ? title : UM_tag2word(C.tags, myLang.code);
          $('#formFields').append(`
<div class="field">
  <label for="${i + 1}">${hint}</label>            
  <div class="field-nowrap" style="max-width: 300px;">
    <input type="text" id="${i + 1}" data-attr="${ids}" value=${w} />
    <button type="button" id="btnSubmit${
      i + 1
    }" class="smallButton approve" onclick=
    "SubmitThisCell(${i + 1},${ids})" >
      <span class="material-icons">done</span>
    </button>
  </div>
</div>`);
        }
      }
      if (type == 'Lemma') {
        let lemma = data.lemma;
        $('#dataentry').append(
          `<h2>${lemma.entry}</h2><form id="dataForm" dir="${dir}"><div id="formFields"></div></form>`
        );
        for (let i = 0; i < data.pool.length; i++) {
          let C = pool[i];
          let w = WordFromFormula(
            C.formula,
            lemma.entry,
            lemma.stem1,
            lemma.stem2,
            lemma.stem3,
            lemma.stem4,
            C.a
          );
          let title = C.stitle;
          if (C.atitle) {
            title += `, ${C.atitle}`;
          }
          let ids = `${lemma.id},${C.slotid},${C.agreementid ?? 0}`;
          let hint = myLevel == '1' ? title : UM_tag2word(C.tags, myLang.code);
          $('#formFields').append(`
<div class="field">
  <label for="input${i}">${hint}</label>            
  <div class="field-nowrap" style="max-width: 300px;">
    <input type="text" id="${i + 1}" data-attr="${ids}" value=${w} />
    <button type="button" id="btnSubmit${
      i + 1
    }" class="smallButton approve" onclick=
    "SubmitThisCell(${i + 1},${ids})" >
      <span class="material-icons">done</span>
    </button>
  </div>
</div>`);
        }
      }
      if (type == 'NN') {
        $('#dataentry').append(
          `<form id="dataForm" dir="${dir}"><div id="formFields"></div></form>`
        );
        for (let i = 0; i < data.pool.length; i++) {
          let C = pool[i];
          let w = WordFromFormula(
            C.formula,
            C.entry,
            C.stem1,
            C.stem2,
            C.stem3,
            C.stem4,
            C.a
          );
          let title = C.stitle;
          if (C.atitle) {
            title += `| ${C.atitle}`;
          }
          let ids = `${C.lemmaid},${C.slotid},${C.agreementid ?? 0}`;
          let hint = myLevel == '1' ? title : UM_tag2word(C.tags, myLang.code);
          $('#formFields').append(`
<div class="field">
  <label for="input${i}">${hint}</label>            
  <div class="field-nowrap" style="max-width: 300px;">
    <input type="text" id="${i + 1}" data-attr="${ids}" value=${w} />
    <button type="button" id="btnSubmit${
      i + 1
    }" class="smallButton approve" onclick=
    "SubmitThisCell(${i + 1},${ids})" >
      <span class="material-icons">done</span>
    </button>
  </div>
</div>`);
        }
      }

      $('#formFields').append(
        `<button type="button" class="smallButton approve" onclick="submitall('${type}')">
        <span class="material-icons">done</span> SUBMIT ALL!</button>`
      );
      $('.loading').hide();
    },
    error: function (data) {
      alert(data);
    },
  });
}

function WordFromFormula(formula, lemma, stem1, stem2, stem3, stem4, agg) {
  let w = formula;
  w = w.replace(/L/g, lemma);
  w = w.replace(/S1/g, stem1);
  w = w.replace(/S2/g, stem2);
  w = w.replace(/S3/g, stem3);
  w = w.replace(/S4/g, stem4);
  // circumfix agreement
  if (/A.+A/.test(w)) {
    w = w.replace(/A\+(.+)\+A/g, agg.split('#')[0] + '$1' + agg.split('#')[1]);
  } else {
    w = w.replace(/(\+A|A\+)/g, agg);
  }
  w = w.replace(/\+/g, '');
  w = w.replace(/0/g, '');
  return w;
}

function SubmitThisCell(i, lemmaId, slotId, agreementId) {
  let submitted = $('#' + i)
    .val()
    .trim();
  if (submitted === '') {
    $('#' + i).focus();
    return;
  }
  $('.loading').show();
  let cell = {
    langid: myLang.id,
    lemmaid: lemmaId,
    slotid: slotId,
    agreementid: agreementId,
    submitted: submitted,
  };
  $.ajax({
    url: '/Cell/insert',
    type: 'POST',
    data: {
      cell: cell,
      suggestions: suggestionsForThisCell,
    },
    success: function (data) {
      $('.loading').hide();
      // remove input
      $('#' + i).remove();
      $('#btnSubmit' + i).remove();
    },
    error: function (data) {
      alert(data);
    },
  });
}

function submitall(nextType) {
  $('.loading').show();
  const dataToSend = [];
  $('input[type="text"]')
    .filter(function () {
      return /^\d/.test(this.id);
    })
    .each(function () {
      const id = $(this).attr('id');
      const dataAttr = $(this).data('attr');
      let cell = {
        langid: myLang.id,
        lemmaid: dataAttr.split(',')[0],
        slotid: dataAttr.split(',')[1],
        agreementid: dataAttr.split(',')[2],
        submitted: $(this).val(),
      };
      dataToSend.push(cell);
    });
  // disable all the inputs
  $('input').prop('disabled', true);
  $('button').prop('disabled', true);
  $.ajax({
    url: '/Cell/batchInsert',
    method: 'POST',
    contentType: 'application/json',
    data: JSON.stringify(dataToSend),
    success: function (response) {
      $('.loading').hide();
      $('input').prop('disabled', false);
      $('button').prop('disabled', false);
      getNextPage(nextType);
    },
    error: function (data) {
      alert(data);
    },
  });
}

function getNextPage(type) {
  $('#dataentry').html('');
  if (type == 'Lemma') {
    lemmaPage++;
    EntryGetTable(myLang.id, lemmaPage, type);
  } else if (type == 'Slot') {
    slotPage++;
    EntryGetTable(myLang.id, slotPage, type);
  } else {
    nnPage++;
    EntryGetTable(myLang.id, nnPage, type);
  }
}

function getPrevPage(type) {
  $('#dataentry').html('');
  if (type == 'Lemma') {
    if (lemmaPage > 2) {
      lemmaPage--;
      EntryGetTable(myLang.id, lemmaPage, type);
    }
  } else if (type == 'Slot') {
    if (slotPage > 2) {
      slotPage--;
      EntryGetTable(myLang.id, slotPage, type);
    }
  } else {
    if (nnPage > 2) {
      nnPage--;
      EntryGetTable(myLang.id, nnPage, type);
    }
  }
}

function findReplace() {
  let finds = $('#find').val().split('\n');
  let replaces = $('#replace').val().split('\n');
  for (let i = 0; i < finds.length; i++) {
    let find = finds[i];
    let replace = replaces[i];
    $('input[type="text"]').each(function () {
      $(this).val($(this).val().replace(new RegExp(find, 'g'), replace));
    });
  }
}

$(document).ready(function () {
  dir = i18n[myMetalang]['dir'];

  if (myLevel != '0') {
    $('.loading').hide();
    $('#frm-elicit').html(`
<div>
  <div class="progress-container" id="langstats"></div>
  <div class="field">
    <lable>Elicitation order:</lable>
    <select id="cmbElicitOrder">
        <option value="Lemma">By Lemma</option>
        <option value="Slot">By Slot</option>
        <option value="NN">By Priority</option>
    </select>
    <span id="pageButtons"></span>
  </div>
</div>
<details id="findReplace" style="width: 300px; color: #555;">
  <summary><span class="material-icons">find_replace</span> Morphophonemic Rules</summary>
  <div>
    <label>Rule:</label>
    <select id="cmbMorphophonemicRule"></select>
  </div>
  <div class="field2cols">
    <div class="field">
      <label for="find">Find:</label>
      <textarea id="find" name="find"></textarea>
    </div>
    <div class="field">
      <label for="replace">Replace with:</label>
      <textarea id="replace" name="replace"></textarea>
    </div>
  </div>
  <button type="button" onclick="findReplace()">Replace All</button>
</details>
<div id="dataentry"></div>`);
  }
  $('#cmbElicitOrder').change(function () {
    if ($(this).val() == 'NN') {
      $('#pageButtons').html(`
<button type="button" class="primary" onclick="getPrevPage('NN')">< Previous</button>
<button type="button" class="primary" onclick="getNextPage('NN')">Next  ></button>`);
    } else if ($(this).val() == 'Slot') {
      $('#pageButtons').html(`
  <button type="button" class="primary" onclick="getPrevPage('Slot')">< Previous</button>
  <button type="button" class="primary" onclick="getNextPage('Slot')">Next  ></button>`);
    } else {
      $('#pageButtons').html(`
  <button type="button" class="primary" onclick="getPrevPage('Lemma')">< Previous</button>
  <button type="button" class="primary" onclick="getNextPage('Lemma')">Next  ></button>`);
    }
  });
  $('#cmbElicitOrder').change();
  $('#findReplace').hide();
  getRules(myLang.id);

  $('#cmbMorphophonemicRule').change(function () {
    let rule = rules.filter((x) => x.id == $(this).val())[0];
    $('#find').val(rule.replacefrom);
    $('#replace').val(rule.replaceto);
  });

  getNextPage('Lemma');
});
