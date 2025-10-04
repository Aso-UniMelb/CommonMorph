let langs = [];

function getLangList() {
  $('#cmbMetaLangs').append(`<option value="">--Select a language--</option>`);
  $.each(i18n, function (key, value) {
    $('#cmbMetaLangs').append(
      `<option value="${key}">${i18n[key]['name']}</option>`
    );
  });
  $('.loading').hide();
}

$('#cmbMetaLangs').on('change', function (e) {
  $('.loading').show();
  $.ajax({
    url: '/QTemplate/download',
    type: 'GET',
    data: {
      metalang: $('#cmbMetaLangs').val(),
    },
    success: function (data) {
      $('.loading').hide();
      if (data.length == 0) {
        $('.download-elicitation').html(
          `<div class="warning">Sorry! No data is available for this language!</div>`
        );
      } else {
        $('.download-elicitation').html('');
        let dir = i18n[$('#cmbMetaLangs').val()]['dir'];
        for (let i = 0; i < data.length; i++) {
          $('.download-elicitation').append(`<dl>            
            <dt>Features:</div>
            <dd>${UM_tags2DimFeat(data[i].unimorphtags)}</dd>
            <dt>UniMorph:</dt>
            <dd>${data[i].unimorphtags}</dd>
            <dt>Prompt:</dt>
            <dd dir="${dir}">${data[i].question}</dd>
            </dl>`);
        }
      }
    },
    error: function (data) {
      console.log(data);
    },
  });
});

$('#cmbLangs').on('select2:select', function (e) {
  var selectedItem = e.params.data;
  let d = langs.findIndex(function (item, i) {
    return item.id == selectedItem.id;
  });
  $('.loading').show();
});

$('.tab').click(function () {
  var tabId = $(this).data('tab');
  $('.tab').removeClass('active');
  $(this).addClass('active');
  $('.tab-content').removeClass('active');
  $('#' + tabId).addClass('active');
});

$(document).ready(function () {
  getLangList();
});
