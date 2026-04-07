function Analyse() {
  let inputWord = $('#txtWord').val().trim().toLowerCase();
  if (inputWord.length == 0) return;
  $.ajax({
    url: '/Cell/GetAnalyses',
    type: 'POST',
    data: {
      word: inputWord
    },
    success: function (data) {
      let html = `<h2>Results for "${inputWord}":</h2>`;
      if (data.length == 0) {
        html += `<p>No results found.</p>`;
      } else {
        html += `<table class="analyses"><thead><tr><th>Language</th><th>Lemma</th><th>Form</th><th>Morphosyntactic Feature Set</th></tr></thead><tbody>`;
        for (let i = 0; i < data.length; i++) {
          let tags = data[i].Tags;
          let format = $('input[name="tagsFormat"]:checked').val();
          if (format == 'universal') {
            tags = UM_tag2word(tags);
          }
          else {
            tags = `<code class="umtag">${data[i].Tags}</code>`;
          }
          html += `<tr><td>${data[i].LanguageName}</td><td>${data[i].Lemma}</td><td>${data[i].Form}</td><td>${tags}</td></tr>`;
        }
        html += `</tbody></table>`;
      }
      $('#results').html(html);
    },
    error: function (data) {
      alert('Error');
    },
  });
}

$(document).ready(function () {
  $('#btnAnalyse').click(function () {
    Analyse();
  });
  $('input[name="tagsFormat"]').change(function () {
    Analyse();
  });
});