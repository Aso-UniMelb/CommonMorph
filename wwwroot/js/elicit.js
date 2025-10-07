let page = 1;
let suggestionsForThisCell = [];
function getForEntry(myLangId, pg) {
  $('.loading').show();
  $('#frm-elicit').hide();
  $.ajax({
    url: '/Elicit/listForEntry',
    type: 'GET',
    data: {
      langid: myLangId,
      page: pg,
      metalang: myMetalang,
    },
    success: function (data) {
      if (data.q) {
        let dir = i18n[myMetalang]['dir'];
        $('#frm-elicit').html('');
        let C = data.r;
        // === suggestion from formula
        let w = C.formula;
        w = w.replace(/L/g, C.lemma);
        w = w.replace(/S1/g, C.stem1);
        w = w.replace(/S2/g, C.stem2);
        w = w.replace(/S3/g, C.stem3);
        w = w.replace(/(\+A|A\+)/g, C.a);
        w = w.replace(/\+/g, '');
        w = w.replace(/0/g, '');
        suggestionsForThisCell = []; // reset
        suggestionsForThisCell.push({ source: 'f', suggested: w });
        // Showing lemma and features for expert speakers
        let features = C.stitle;
        if (typeof C.atitle !== 'undefined') {
          features += ` ${C.atitle}`;
        }

        // Showing Questions for non-expert speakers
        let q = data.q.question;
        q = q.replace(/XXX/g, `<b>${C.eng}/${C.lemma}</b>`);
        $('#frm-elicit').append(
          `<div class="elicit_questions" dir="${dir}">
            <h3>${i18n[myMetalang]['elicit_instructions']}</h3>
            <div dir="${dir}" class="question"><span class="material-icons">help</span><div>${q}</div></div>
          </div>`
        );
        // showing suggestions
        $('.elicit_questions').append(`
          <div class="suggestions" id="suggestions" dir="${dir}">
            <span>${i18n[myMetalang]['elicit_suggestions']}</span>
            <button type="button" class="suggestedForm" onclick="acceptSuggestion('${w}')">${w}</button>
          </div>`);
        // showing form to submit the form
        $('.elicit_questions').append(`
          <div class="elicit-submit" dir="${dir}">
            <div class="field-nowrap">
              <label>${i18n[myMetalang]['elicit_answer']}:</label>
              <input type="text" id="txtSubmittingForm" required>
              <button type="button" onclick="SubmitCell(${C.lemmaid}, ${C.slotid}, ${C.agreementid})" class="approve" >
                <span class="material-icons">done</span>
              </button>
            </div>
            <button type="button" onclick="nextpage()">${i18n[myMetalang]['elicit_skip']} <span class="material-icons">skip_next</span></button>
          </div>`);
        // if DB contains inflected forms from the same morpho-syntactic tag set:
        if (data.s.length > 0) {
          let samples = data.s;
          // 1. show previous samples for more context for the speakers
          $('#frm-elicit')
            .append(`<div id="samples" dir="${dir}" class="samples">
            <span>${i18n[myMetalang]['elicit_samples']}:</span>
            </div>`);
          for (let i = 0; i < samples.length; i++) {
            $('#samples').append(`<div class="sample">
              <span class="inflected">${samples[i].form}</span>
              (${i18n[myMetalang]['elicit_from']}: <span class="lemma">${samples[i].lemma}</span>)
              </div>`);
          }
          // 2. prepare data for API call for suggestion
          let curLemma = {
            lemma: C.lemma,
            stem1: C.stem1,
            stem2: C.stem2,
            stem3: C.stem3,
          };
          // 3. API call
          getSuggestionFromLLM(curLemma, samples);
          // 4. suggestion from NN
          getSuggestionFromNN(C.lemma, UM_Sort(C.tags));
        }

        $('#frm-elicit').append(`
          <div class="elicit_expert">
            <div class="lemma"><b>${C.lemma}</b> | ${features}</div>  
            <div class="features">${UM_tag2word(C.tags, myLang.code)}</div>     
          </div>`);
      } else {
        $('#frm-elicit').slideDown();
        $('#frm-elicit').html(
          `<div class="done">${i18n[myMetalang]['elicit_end']}</div>`
        );
      }
      $('#frm-elicit').slideDown();
      $('.loading').hide();
    },
    error: function (data) {
      alert(data);
    },
  });
}

function getSuggestionFromNN(lemma, tags) {
  $('.loading').show();
  $.ajax({
    url: '/ActiveLearning/suggest',
    type: 'POST',
    data: {
      langid: myLang.id,
      lemma: lemma,
      tags: tags,
    },
    success: function (data) {
      console.log(data);
      let R = JSON.parse(data);
      let pr = '';
      for (let i = 0; i < R.confidence.length; i++) {
        let sahde = Math.ceil(R.confidence[i] * 200);
        pr += `<span style="background:rgb(${
          200 - sahde
        }, ${sahde}, 0);padding: 3px 0;">${R.predicted[i]}</span>`;
      }
      $('#suggestions').append(
        `<button type="button" class="suggestedForm" onclick="acceptSuggestion('${R.predicted}')"><div>${pr}</div></button>`
      );
    },
    error: function (data) {
      console.log(data);
    },
  });
}

function getSuggestionFromLLM(curLemma, samples) {
  $('.loading').show();
  $.ajax({
    url: '/LLM/getSuggestionFromLLM',
    type: 'POST',
    data: {
      curLemma: curLemma,
      samples: samples,
    },
    success: function (data) {
      $.each(data, function (key, value) {
        if (!suggestionsForThisCell.some((s) => s.suggested == value)) {
          $('#suggestions').append(
            `<button type="button" class="suggestedForm" onclick="acceptSuggestion('${value}')">${value}</button>`
          );
        }
        suggestionsForThisCell.push({
          source: key,
          suggested: value,
          shots: samples.length,
        });
      });
      $('.loading').hide();
    },
    error: function (data) {
      console.log(data);
    },
  });
}

function acceptSuggestion(suggestion) {
  $('#txtSubmittingForm').val(suggestion);
}

function SubmitCell(lemmaId, slotId, agreementId) {
  let submitted = $('#txtSubmittingForm').val().trim();
  if (submitted === '') {
    $('#txtSubmittingForm').focus();
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
      getForEntry(myLang.id, lemmaPage);
    },
    error: function (data) {
      alert(data);
    },
  });
}

// if get focus, check if the text box is empty, border should be red
$('#txtSubmittingForm').on('focus', function () {
  if ($(this).val() === '') {
    $(this).css('border', '2px solid red');
  } else {
    $(this).css('border', '1px solid #ccc');
  }
});

function nextpage() {
  $('#frm-elicit').html('');
  lemmaPage++;
  getForEntry(myLang.id, lemmaPage);
}

$(document).ready(function () {
  $('#drawer-elicit').addClass('active');
  $('#elicit').html(`
<div class="field">
  <label>My grammar knowledge:</label>
  <select id="cmbLevel" style="width: 200px;">
    <option value="0">Not confident in my grammar skills</option>
    <option value="1">Basic (high school grammar)</option>
    <option value="2">Expert (familiar with linguistics terms)</option>
  </select>
</div><div id="frm-elicit"></div>`);
  myLevel = localStorage.getItem('myLevel');
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
    getForEntry(myLang.id, 1);
  }
});
