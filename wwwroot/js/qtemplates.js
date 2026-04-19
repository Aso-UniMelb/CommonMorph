let page = 1;
let UniMorphTags = '';
let dir = 'ltr';
let allQuestions = [];
let currentTags = '';
let isCurrentAvailable = false;

function getQuestionLists() {
  $('#questionLists').html('<div style="padding: 20px; text-align: center;">Loading questions...</div>');
  renderForm();

  // Fetch both available and unavailable questions
  Promise.all([
    fetchQuestions('/QTemplate/available', true),
    fetchQuestions('/QTemplate/unavailable', false),
  ])
    .then((results) => {
      allQuestions = [...results[0], ...results[1]];
      // Sort alphabetically by title
      allQuestions.sort((a, b) => a.stitle.localeCompare(b.stitle));
      renderQuestionList(allQuestions);
    })
    .catch((err) => {
      console.error('Error fetching questions:', err);
      $('#questionLists').html('<div style="padding: 20px; color: red;">Failed to load questions.</div>');
    });
}

function fetchQuestions(url, isAvailable) {
  return new Promise((resolve, reject) => {
    $.ajax({
      url: url,
      type: 'GET',
      data: {
        metalang: myMetalang,
        langId: myLang.id,
      },
      success: function (data) {
        if (typeof data !== 'undefined') {
          resolve(data.map((q) => ({ ...q, available: isAvailable })));
        } else {
          resolve([]);
        }
      },
      error: reject,
    });
  });
}

function renderQuestionList(questions) {
  let html = '';
  if (questions.length === 0) {
    html = '<div style="padding: 20px; color: #888; text-align: center;">No questions found.</div>';
  } else {
    questions.forEach((q) => {
      let title = q.stitle + (q.atitle ? ` (${q.atitle})` : '');
      let statusIcon = q.available ? 'check_circle' : 'radio_button_unchecked';
      let statusClass = q.available ? 'available' : 'unavailable';
      let activeClass = currentTags === q.tags ? 'active' : '';

      html += `
        <div class="qTemplate-item ${statusClass} ${activeClass}" onclick="selectQuestion('${q.tags}', ${q.available}, this)">
          <span class="material-icons status-icon">${statusIcon}</span>
          <div class="item-content">
            <div class="item-title" title="${title}">${title}</div>
            <div class="item-tags" title="${q.tags}">${q.tags}</div>
          </div>
        </div>`;
    });
  }
  $('#questionLists').html(html);
}

function renderForm() {
  let q_submit = i18n[myMetalang]['q_submit'];
  let elicit_skip = i18n[myMetalang]['elicit_skip'];

  $('#questionForm').html(`
<form id="form" onsubmit="QTemplateSubmit(); return false;" dir="${dir}">
  <div class="inflectional_features"></div>
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
    <button type="button" onclick="nextStructure()">
      ${elicit_skip} <span class="material-icons">skip_next</span>
    </button>
  </div>
</form>`);
  $('#questionForm').hide();
  $('#instruction').html(i18n[myMetalang]['q_4']).hide();
}

function selectQuestion(tags, available, element) {
  currentTags = tags;
  isCurrentAvailable = available;
  
  $('.qTemplate-item').removeClass('active');
  $(element).addClass('active');

  $('#unimorphtags').val(tags);
  $('#examples').html('');
  showFeatures(tags);

  if (available) {
    showAvailableQuestion(tags);
  } else {
    $('#question').val('');
    $('#questionForm').show();
    $('#instruction').show();
  }

  // Show the example question from LLM
  let prompt = UM_tags2QuestionPrompt(tags, myMetalang, myLang.Code);
  getQTemplateFromLLM(prompt);
}

function showFeatures(tags) {
  $('.inflectional_features').html('');
  let feats = UM_tags2DimFeat(tags, myLang.Code);
  for (let i = 0; i < feats.length; i++) {
    $('.inflectional_features').append(`<span>${feats[i]}</span>`);
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
      $('.loading').hide();
    },
  });
}

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
      $('.loading').hide();
    },
  });
}

function QTemplateSubmit() {
  $('.loading').show();
  let url = isCurrentAvailable ? 'update' : 'insert';
  $.ajax({
    url: '/QTemplate/' + url,
    type: 'POST',
    data: {
      questionlang: $('#questionlang').val(),
      unimorphtags: $('#unimorphtags').val(),
      question: $('#question').val(),
    },
    success: function (data) {
      $('.loading').hide();
      // Refresh the specific item in the list or just reload all
      nextStructure();
    },
    error: function (data) {
      console.log('Error');
      $('.loading').hide();
    },
  });
}

function nextStructure() {
  currentTags = '';
  getQuestionLists();
}

$(document).ready(function () {
  $('#drawer-questions').addClass('active');
  $('.loading').hide();
  dir = i18n[myMetalang]['dir'];
  getQuestionLists();

  // Search functionality
  $('#qSearch').on('input', function () {
    let val = $(this).val().toLowerCase();
    let filtered = allQuestions.filter(
      (q) =>
        q.tags.toLowerCase().includes(val) ||
        q.stitle.toLowerCase().includes(val) ||
        (q.atitle && q.atitle.toLowerCase().includes(val))
    );
    renderQuestionList(filtered);
  });
});
