let lemmaPage = 0;
let slotPage = 0;
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

      if (type == 'Slot') {
        let S = data.slot;
        $('#checking').append(
          `<h2>${S.title}</h2>
          <form id="dataForm" dir="${dir}"><div id="formFields"></div></form>`
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
  <label for="input${i}">${hint}</label>            
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
          `<h2>${L.entry}</h2>
          <form id="dataForm" dir="${dir}"><div id="formFields"></div></form>`
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
  <label for="input${i}">${hint}</label>            
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
        `<button type="button" class="smallButton approve" onclick="ApproveAll()">
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
  $('input[type="text"]')
    .filter(function () {
      return /^\d/.test(this.id);
    })
    .each(function () {
      const id = $(this).attr('id');
      const dataAttr = $(this).data('attr');
      let cell = {
        cellid: dataAttr.split(',')[0],
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
      $('input').prop('disabled', false);
      $('button').prop('disabled', false);
      getNextPage(type);
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
    slotPage++;
    CheckGetTable(myLang.id, slotPage, type);
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
    if (slotPage > 1) {
      slotPage--;
      CheckGetTable(myLang.id, slotPage, type);
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
        <option value="Slot">By Slot</option>
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
  getNextPage('Lemma');
});
