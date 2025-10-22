let page = 1;

function getCellForCheck(id, pg) {
  $('.loading').show();
  $('#frm-check').hide();
  $.ajax({
    url: '/Elicit/listForCheck',
    type: 'GET',
    data: { langid: id, page: pg, metalang: myMetalang },
    success: function (data) {
      if (data.q) {
        let dir = i18n[myMetalang]['dir'];
        let C = data.r;
        let features = C.stitle;
        if (typeof C.atitle !== 'undefined') {
          features += ` ${C.atitle}`;
        }
        $('#frm-check').html('');
        // Showing Questions for non-expert speakers
        let q = data.q.question;
        q = q.replace(/XXX/g, `<b>${C.eng}/${C.lemma}</b>`);
        c = $('#frm-check').append(
          `<div class="elicit_questions" dir="${dir}">
            <div class="question"><span class="material-icons">help</span><div>${q}</div></div>
            <div class="submitted" dir="${dir}">
            ${i18n[myMetalang]['check_instructions'].replace(
              'XXX',
              `«<span>${C.submitted}</span>»`
            )}
            </div>
          </div>`
        );
        $('.elicit_questions').append(`          
          <div class="elicit-submit" dir="${dir}">
            <button type="button" onclick="Approve(${C.cellid})" class="approve">
            ${i18n[myMetalang]['check_yes']}
              <span class="material-icons">check_circle</span>
            </button>
            <button type="button" onclick="Disapprove(${C.cellid})" class="disapprove">
            ${i18n[myMetalang]['check_no']}
              <span class="material-icons">cancel</span>
            </button>
            <button type="button" onclick="nextpage()">${i18n[myMetalang]['elicit_skip']} <span class="material-icons">skip_next</span></button>
          </div>`);
        $('#frm-check').append(`
          <div class="elicit_expert">
            <div class="lemma"><b>${C.lemma}</b> | ${features}</div>  
            <div class="features">${UM_tag2word(C.tags, myLang.code)}</div>     
          </div>`);
      } else {
        $('#frm-check').slideDown();
        $('#frm-check').html(
          `<div class="done">${i18n[myMetalang]['elicit_end']}</div>`
        );
      }

      $('#frm-check').slideDown();
      $('.loading').hide();
    },
    error: function (data) {
      alert(data);
    },
  });
}

function Approve(id) {
  $.ajax({
    url: '/Cell/approve',
    type: 'POST',
    data: {
      cellid: id,
    },
    success: function (data) {
      getCellForCheck(myLang.id, page);
    },
    error: function (data) {
      alert(data);
    },
  });
}

function Disapprove(id) {
  $.ajax({
    url: '/Cell/disapprove',
    type: 'POST',
    data: {
      cellid: id,
    },
    success: function (data) {
      getCellForCheck(myLang.id, page);
    },
    error: function (data) {
      alert(data);
    },
  });
}

function nextpage() {
  $('#frm-check').html('');
  page++;
  getCellForCheck(myLang.id, page);
}

$(document).ready(function () {
  $('#drawer-check').addClass('active');
  $('#check').html(`
<div class="field">
  <label>My grammar knowledge:</label>
  <select id="cmbLevel" style="width: 300px;">
    <option value="0">Not confident in my grammar skills</option>
    <option value="1">Basic (high school grammar)</option>
    <option value="2">Expert (familiar with linguistics terms)</option>
  </select>
</div><div id="frm-check"></div>`);
  const myLevel = localStorage.getItem('myLevel');
  if (myLevel) {
    $('#cmbLevel').val(myLevel).trigger('change');
  } else {
    localStorage.setItem('myLevel', $('#cmbLevel').val());
  }

  $('#cmbLevel').change(function () {
    localStorage.setItem('myLevel', $('#cmbLevel').val());
    window.location.reload();
  });
  if (myLevel == '0') {
    getCellForCheck(myLang.id, 1);
  }
});
