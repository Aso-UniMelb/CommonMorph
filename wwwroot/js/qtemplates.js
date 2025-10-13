let page = 1;
let UniMorphTags = '';
let dir = 'ltr';

function getQuestionLists() {
  // prepare the page elements
  // questionLists
  // fetch the data from the server
  $('#questionLists').html('');
  $('#questionLists').append(`<div class="field">
    <label>${i18n[myMetalang]['q_1']}</label>
    <select id="cmbAvailable" style="width: 350px;">
      <option value="">-</option>
    </select></div>`);

  $('#questionLists').append(`<div class="field">
    <label>${i18n[myMetalang]['q_2']}</label>
    <select id="cmbUnavailable" style="width: 350px;">
      <option value="">-</option>
    </select></div>`);
  getListAvailable();
  getListUnavailable();

  // questionForm
  let q_submit = i18n[myMetalang]['q_submit'];
  let elicit_skip = i18n[myMetalang]['elicit_skip'];

  $('#questionForm').html(`
<form id="form" onsubmit="QTemplateSubmit(); return false;" dir="${dir}">
  <div class="paradigm_features"></div>
  ${i18n[myMetalang]['q_3']}
  <input type="hidden" id="questionlang" value="${myMetalang}" />
  <input type="hidden" id="unimorphtags" value="" />
  <div class="field">
    <textarea id="question" name="question" rows="3" placeholder="Your prompt here" required></textarea>
  </div>
  <div class="field-nowrap">
    <button type="submit">
      ${q_submit} <span class="material-icons">send</span>
    </button>
    <button type="button" onclick="nextSlot()">
      ${elicit_skip} <span class="material-icons">skip_next</span>
    </button>
  </div>
</form>`);
  $('#questionForm').hide();

  // instruction
  $('#instruction').html(i18n[myMetalang]['q_4']);
  $('#instruction').hide();
  // examples
}

function getListAvailable() {
  $('.loading').show();
  $.ajax({
    url: '/QTemplate/available',
    type: 'GET',
    data: {
      metalang: myMetalang,
      langId: myLang.id,
    },
    success: function (data) {
      $('.loading').hide();
      if (typeof data !== 'undefined') {
        for (let i = 0; i < data.length; i++) {
          let title = data[i].stitle;
          if (data[i].atitle) {
            title += ` (${data[i].atitle})`;
          }
          $('#cmbAvailable').append(
            `<option value="${data[i].tags}">${title}</option>`
          );
        }
        $('#cmbAvailable').select2();
        $('#cmbAvailable').on('select2:select', function (e) {
          $('#cmbUnavailable').val('').trigger('change');
          let tags = $('#cmbAvailable').val();
          $('#unimorphtags').val(tags);
          $('#examples').html('');
          showFeatures(tags);
          showAvailableQuestion(tags);
          //show the example question
          let prompt = UM_tags2QuestionPrompt(tags, myMetalang, myLang.Code);
          getQTemplateFromLLM(prompt, myMetalang);
        });
      }
    },
    error: function (data) {
      console.log(data);
    },
  });
}

function showFeatures(tags) {
  $('.paradigm_features').html('');
  let feats = UM_tags2DimFeat(tags, myLang.Code);
  for (let i = 0; i < feats.length; i++) {
    $('.paradigm_features').append(`<span>${feats[i]}</span>`);
  }
}

function showAvailableQuestion(tags) {
  $('.loading').show();
  $.ajax({
    url: '/QTemplate/get',
    type: 'GET',
    data: {
      metalang: myMetalang,
      tags: tags,
    },
    success: function (data) {
      $('.loading').hide();
      $('#question').val(data);
      $('#examples').html('');
      $('#questionForm').show();
      $('#instruction').show();
    },
    error: function (data) {
      console.log(data);
    },
  });
}

function getListUnavailable() {
  $('.loading').show();
  $('#examples').html('');
  $.ajax({
    url: '/QTemplate/unavailable',
    type: 'GET',
    data: {
      metalang: myMetalang,
      langId: myLang.id,
    },
    success: function (data) {
      $('.loading').hide();
      for (let i = 0; i < data.length; i++) {
        let title = data[i].stitle;
        if (data[i].atitle) {
          title += ` (${data[i].atitle})`;
        }
        $('#cmbUnavailable').append(
          `<option value="${data[i].tags}">${title}</option>`
        );
      }
      $('#cmbUnavailable').select2();
      $('#cmbUnavailable').on('select2:select', function (e) {
        $('#cmbAvailable').val('').trigger('change');
        $('#questionForm').show();
        $('#instruction').show();
        let tags = $('#cmbUnavailable').val();
        showFeatures(tags);
        $('#unimorphtags').val(tags);
        $('#question').val('');
        //show the example question
        let prompt = UM_tags2QuestionPrompt(tags, myMetalang, myLang.Code);
        getQTemplateFromLLM(prompt, myMetalang);
      });
    },
    error: function (data) {
      console.log(data);
    },
  });
}

let number_of_questions = 0;
function getQTemplateFromLLM(prompt) {
  $('.loading').show();
  $('#examples').html(`<div class="q-LLM">${i18n[myMetalang]['q_5']}</div>`);

  $.ajax({
    url: '/LLM/getQuestionFromLLM',
    type: 'POST',
    data: {
      prompt: prompt,
    },
    success: function (data) {
      $('.question').remove();
      $.each(data, function (key, value) {
        let q = value;
        let model = key;
        q = q.replace(/\n/g, '  ');
        q = q.replace(/"/g, "'");
        q = q.trim();
        $('#examples').append(
          `<div class="question" dir="${dir}"><small>${model}:</small><blockquote>${q}</blockquote> </div>`
        );
      });
      $('.loading').hide();
    },
    error: function (data) {
      console.log(data);
    },
  });
}

function QTemplateSubmit() {
  $('.loading').show();
  let url = $('#cmbAvailable').val() ? 'update' : 'insert';
  $.ajax({
    url: '/QTemplate/' + url,
    type: 'POST',
    data: {
      questionlang: $('#questionlang').val(),
      unimorphtags: $('#unimorphtags').val(),
      question: $('#question').val(),
    },
    success: function (data) {
      console.log(data);
      nextSlot();
    },
    error: function (data) {
      console.log('Error');
    },
  });
}

function nextSlot() {
  $('#questionLists').html('');
  getQuestionLists();
}

$(document).ready(function () {
  $('#drawer-questions').addClass('active');
  $('.loading').hide();
  dir = i18n[myMetalang]['dir'];
  getQuestionLists();
});
