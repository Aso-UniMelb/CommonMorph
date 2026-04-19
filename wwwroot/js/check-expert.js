let lemmaPage = 0;
let structurePage = 0;
let dir = 'ltr';

function CheckGetTable(myLangId, pg, type) {
  $('.loading').show();
  $('#checking').html('');
  $.ajax({
    url: '/Elicit/CheckGetTableBy' + type,
    type: 'GET',
    data: {
      langid: myLangId,
      page: pg,
    },
    success: function (data) {
      if (data.length == 0) {
        $('#checking').html(
          `<div class="done">${i18n[myMetalang]['elicit_end']}</div>`
        );
        $('.loading').hide();
        return;
      }
      let pool = data.pool;

      if (type == 'Structure') {
        let S = data.structure;
        $('#checking').append(
          `<h2>${S.title}</h2>
          <form id="dataForm"><div id="formFields"></div></form>`
        );
        for (let i = 0; i < pool.length; i++) {
          let C = pool[i];
          let title = S.title;
          if (C.atitle) {
            title += `, ${C.atitle}`;
          }
          title += ' (' + C.lemma + ')';
          let hint = myLevel == '1' ? title : UM_tag2word(C.tags, myLang.code);
          $('#formFields').append(`
<div class="field">
  <label>${hint}</label>            
  <div class="field-nowrap" style="max-width: 300px;" id="${C.cellid}">
    <input type="text" 
       data-attr="${C.cellid}" value=${C.submitted} disabled />
    <button type="button" id="btnApprove${C.cellid}" 
      class="smallButton approve"
      onclick="RateThisCell(${C.cellid}, 'approve')" >
      <span class="material-icons">done</span>
    </button>
    <button type="button" id="btnDisapprove${C.cellid}" 
      class="smallButton disapprove"
      onclick="RateThisCell(${C.cellid}, 'disapprove')" >
      <span class="material-icons">close</span>
    </button>
  </div>
</div>`);
        }
      }
      if (type == 'Lemma') {
        let L = data.lemma;
        $('#checking').append(
          `<h2>${L.entry} <small>(${L.engmeaning})</small></h2>
          <a href="https://en.wiktionary.org/wiki/${L.entry}" target="_blank">Wiktionary</a>
          <form id="dataForm"><div id="formFields"></div></form>`
        );
        for (let i = 0; i < pool.length; i++) {
          let C = pool[i];
          let title = C.stitle;
          if (C.atitle) {
            title += `| ${C.atitle}`;
          }
          let hint = myLevel == '1' ? title : UM_tag2word(C.tags, myLang.code);
          $('#formFields').append(`
<div class="field">
  <label>${hint}</label>            
  <div class="field-nowrap" style="max-width: 300px;" id="${C.cellid}" >
    <input type="text" 
      data-attr="${C.cellid}" value=${C.submitted} disabled />
    <button type="button" id="btnApprove${i + 1}" 
      class="smallButton approve"
      onclick="RateThisCell(${C.cellid}, 'approve')" >
      <span class="material-icons">done</span>
    </button>
    <button type="button" id="btnDisapprove${i + 1}" 
      class="smallButton disapprove"
      onclick="RateThisCell(${C.cellid}, 'disapprove')" >
      <span class="material-icons">close</span>
    </button>
  </div>
</div>`);
        }
      }
      $('#formFields').append(
        `<button type="button" class="smallButton approve" onclick="ApproveAll('${type}')">
          <span class="material-icons">done</span> Approve ALL
        </button>`
      );
      $('.loading').hide();
    },
    error: function (data) {
      alert(data);
    },
  });
}

function ApproveAll(nextType) {
  $('.loading').show();
  const dataToSend = [];
  $('input[type="text"]').each(function () {
    const id = $(this).attr('id');
    const dataAttr = $(this).data('attr');
    let cell = {
      cellid: dataAttr,
    };
    dataToSend.push(cell);
  });
  console.log(JSON.stringify(dataToSend));
  // disable all the inputs
  $('input').prop('disabled', true);
  $('button').prop('disabled', true);
  $.ajax({
    url: '/Cell/batchApprove',
    method: 'POST',
    contentType: 'application/json',
    data: JSON.stringify(dataToSend),
    success: function (response) {
      $('.loading').hide();
      $('button').prop('disabled', false);
      getNextPage(nextType);
    },
    error: function (data) {
      alert(data);
    },
  });
}

function RateThisCell(cellid, rate) {
  $('#btnDisapprove' + cellid).disabled = true;
  $('#btnApprove' + cellid).disabled = true;
  $.ajax({
    url: '/Cell/' + rate,
    type: 'POST',
    data: {
      cellid: cellid,
    },
    success: function (data) {
      // remove input
      $('#' + cellid).remove();
      $('#btnApprove' + cellid).remove();
      $('#btnDisapprove' + cellid).remove();
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
    CheckGetTable(myLang.id, lemmaPage, type);
  } else {
    structurePage++;
    CheckGetTable(myLang.id, structurePage, type);
  }
}
function getPrevPage(type) {
  $('#dataentry').html('');
  if (type == 'Lemma') {
    if (lemmaPage > 1) {
      lemmaPage--;
      CheckGetTable(myLang.id, lemmaPage, type);
    }
  } else {
    if (structurePage > 1) {
      structurePage--;
      CheckGetTable(myLang.id, structurePage, type);
    }
  }
}

$(document).ready(function () {
  dir = i18n[myMetalang]['dir'];

  if (myLevel != '0') {
    $('.loading').hide();
    $('#frm-check').html(`
<div>
  <div class="field">
    <lable>Chcking order:</lable>
    <select id="cmbElicitOrder">
        <option value="Lemma">By Lemma</option>
        <option value="Structure">By Structure</option>
    </select>
    <span id="pageButtons"></span>
  </div>
</div>
<div id="checking"></div>`);
  }
  $('#cmbElicitOrder').change(function () {
    if ($(this).val() == 'NN') {
      $('#pageButtons').html(`
<button type="button" class="primary" onclick="getPrevPage('NN')">< Previous</button>
<button type="button" class="primary" onclick="getNextPage('NN')">Next  ></button>`);
    } else if ($(this).val() == 'Structure') {
      $('#pageButtons').html(`
  <button type="button" class="primary" onclick="getPrevPage('Structure')">< Previous</button>
  <button type="button" class="primary" onclick="getNextPage('Structure')">Next  ></button>`);
    } else {
      $('#pageButtons').html(`
  <button type="button" class="primary" onclick="getPrevPage('Lemma')">< Previous</button>
  <button type="button" class="primary" onclick="getNextPage('Lemma')">Next  ></button>`);
    }
  });
  $('#cmbElicitOrder').change();
  getNextPage('Lemma');
});
