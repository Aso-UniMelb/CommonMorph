Rules = [];
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
      Rules = data;
      let html = '';
      for (let i = 0; i < Rules.length; i++) {
        html += `<li class="rule" onclick="EditRule(${i})">${Rules[i].title}</li>`;
      }
      $('#lstMorphophonemicRules').html(html);
    },
    error: function (data) {
      console.log(data);
    },
  });
}

$('#btnAddMorphophonemicRule').click(function () {
  $('#frmMorphophonemicRule').show();
  $('#txtMorphophonemicRuleTitle').val('');
  $('#txtMorphophonemicRuleReplaceFrom').val('');
  $('#txtMorphophonemicRuleReplaceTo').val('');
  $('#MorphophonemicRuleId').val('');
  $('#btnMorphophonemicRuleSubmit').html('Add');
});

// sync scrolls
$('#txtMorphophonemicRuleReplaceFrom').on('scroll', function () {
  $('#txtMorphophonemicRuleReplaceTo').scrollTop(
    $('#txtMorphophonemicRuleReplaceFrom').scrollTop()
  );
});

$('#txtMorphophonemicRuleReplaceTo').on('scroll', function () {
  $('#txtMorphophonemicRuleReplaceFrom').scrollTop(
    $('#txtMorphophonemicRuleReplaceTo').scrollTop()
  );
});

function MorphophonemicRuleSubmit() {
  if ($('#btnMorphophonemicRuleSubmit').html() == 'Add') {
    insertRule();
  } else {
    updateRule();
  }
}

function insertRule() {
  $.ajax({
    url: '/Morphophonology/insert',
    type: 'POST',
    data: {
      LangId: myLang.id,
      Title: $('#txtMorphophonemicRuleTitle').val().trim(),
      ReplaceFrom: $('#txtMorphophonemicRuleReplaceFrom').val().trim(),
      ReplaceTo: $('#txtMorphophonemicRuleReplaceTo').val().trim(),
    },
    success: function (data) {
      $('#frmMorphophonemicRule').hide();
      getRules(myLang.id);
    },
    error: function (data) {
      alert('Error');
    },
  });
}

function updateRule() {
  $.ajax({
    url: '/Morphophonology/update',
    type: 'POST',
    data: {
      Id: $('#MorphophonemicRuleId').val(),
      LangId: myLang.id,
      Title: $('#txtMorphophonemicRuleTitle').val().trim(),
      ReplaceFrom: $('#txtMorphophonemicRuleReplaceFrom').val().trim(),
      ReplaceTo: $('#txtMorphophonemicRuleReplaceTo').val().trim(),
    },
    success: function (data) {
      $('#frmMorphophonemicRule').hide();
      getRules(myLang.id);
    },
    error: function (data) {
      alert('Error');
    },
  });
}

$('#btnMorphophonemicRuleCancel').click(function () {
  $('#frmMorphophonemicRule').hide();
});

function EditRule(id) {
  $('#frmMorphophonemicRule').show();
  $('#btnMorphophonemicRuleSubmit').html('Save');
  $('#txtMorphophonemicRuleTitle').val(Rules[id].title);
  $('#txtMorphophonemicRuleReplaceFrom').val(Rules[id].replacefrom);
  $('#txtMorphophonemicRuleReplaceTo').val(Rules[id].replaceto);
  $('#MorphophonemicRuleId').val(Rules[id].id);
  $('#btnMorphophonemicRuleSubmit').html('Save');
}

$(document).ready(function () {
  $('#frmMorphophonemicRule').hide();
  getRules(myLang.id);
});
