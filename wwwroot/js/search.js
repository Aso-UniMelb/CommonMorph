function Analyse() {
  let form = $('#txtForm').val().trim().toLowerCase();
  let lemma = $('#txtLemma').val().trim().toLowerCase();
  let meaning = $('#txtMeaning').val().trim().toLowerCase();
  let tags = $('#txtTags').val().trim().toLowerCase();

  if (form.length == 0 && lemma.length == 0 && meaning.length == 0 && tags.length == 0) {
    $('#results').html('');
    return;
  }
  
  $.ajax({
    url: '/Cell/GetAnalyses',
    type: 'POST',
    data: {
      form: form,
      lemma: lemma,
      meaning: meaning,
      tags: tags
    },
    success: function (data) {
      let html = `<h2>Search Results:</h2>`;
      if (data.length == 0) {
        html += `<p>No results found.</p>`;
      } else {
        html += `<table class="search_results"><thead><tr>
        <th>Language Variety</th>
        <th>Lemma Gloss</th>
        <th>Lemma Entry</th>
        <th>Inflected Form</th>
        <th>Morphosyntactic Tags</th>
        </tr></thead><tbody>`;
        for (let i = 0; i < data.length; i++) {
          let rTags = data[i].Tags;
          let format = $('input[name="tagsFormat"]:checked').val();
          if (format == 'universal') {
            rTags = UM_tag2word(rTags);
          }
          else {
            rTags = `<code class="umtag">${data[i].Tags}</code>`;
          }
          let meaningDisplay = data[i].Meaning == null ? "" : data[i].Meaning;
          html += `<tr><td>${data[i].LanguageName}</td><td>${meaningDisplay}</td><td>${data[i].Lemma}</td><td>${data[i].Form}</td><td>${rTags}</td></tr>`;
        }
        html += `</tbody></table>`;
      }
      $('#results').html(html);
    },
    error: function (data) {
      alert('Error connecting to the server.');
    },
  });
}

$(document).ready(function () {
  $('#btnSearch').click(function (e) {
    e.preventDefault();
    Analyse();
  });
  $('input[name="tagsFormat"]').change(function () {
    Analyse();
  });
});