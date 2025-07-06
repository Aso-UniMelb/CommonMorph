function SurveySubmit() {
  $.ajax({
    url: '/Survey/insert',
    type: 'POST',
    data: {
      langid: myLang.id,
      role: $('#Role').val(),
      q1: $('#LearningTime').val(),
      q2: $('#ContributingTime').val(),
      q3: $('input[name="ExpertLevel"]:checked').val(),
      q4: $('input[name="PedagogicalScore"]:checked').val(),
      q5: $('input[name="EfficiencyScore"]:checked').val(),
      q6: $('input[name="SatisfactionScore"]:checked').val(),
      q7: $('input[name="EaseScore"]:checked').val(),
      q8: '',
      q9: '',
      feedback: $('#feedback').val(),
    },
    success: function (data) {
      alert('Thank you for your feedback!');
      window.location.href = '/app/dashboard';
    },
    error: function (data) {
      alert(data);
    },
  });
}

$(document).ready(function () {
  $('#drawer-survey').addClass('active');
  $('.loading').hide();
  $('#langDisplay').html(myLang.title);
  $('#langId').val(myLang.id);
});
