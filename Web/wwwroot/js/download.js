let langs = [];

function getLangList() {
  $('#cmbMetaLangs').append(`<option value="">--Select a language--</option>`);
  $.each(i18n, function (key, value) {
    $('#cmbMetaLangs').append(
      `<option value="${key}">${i18n[key]['name']}</option>`
    );
  });
  $('.loading').show();
  $.ajax({
    url: '/Lang/list',
    type: 'GET',
    success: function (data) {
      $('.loading').hide();
      langs = data.sort((a, b) => a.title.localeCompare(b.title));
      var options = [];
      options.push({ id: 0, text: 'select' });
      for (let i = 0; i < langs.length; i++) {
        options.push({
          id: langs[i].id,
          text: `[${langs[i].code}] ${langs[i].title}`,
        });
      }
      $('#cmbLangs').select2({ data: options });
    },
    error: function (data) {
      console.log(data);
    },
  });
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

function showUnimorph(langId) {
  $('.loading').show();
  $.ajax({
    url: '/Cell/download',
    type: 'POST',
    data: {
      LangId: langId,
    },
    success: function (data) {
      $('.loading').hide();
      // move scroll to download
      if (data.length == 0) {
        $('.download').html(
          `<div class="warning">Sorry! No approved data is available for this language variety!</div>`
        );
      } else {
        $('.download').html('');
        let table = $('<table>').append(
          $('<thead>').append(
            $('<tr>').append(
              $('<th>').text('LEMMA'),
              $('<th>').text('FORM'),
              $('<th>').text('TAGS')
              //$("<th>").text("#Approved")
            )
          ),
          $('<tbody>')
        );
        $('.download').append(table);
        for (let i = 0; i < data.length; i++) {
          let tags = data[i].stags;
          if (data[i].atags) {
            tags += ';' + data[i].atags;
          }
          let row = `<tr>
            <td>${data[i].lemma}</td>
            <td>${data[i].form}</td>
            <td>${UM_Sort(tags)}</td>
          </tr>`;
          $('tbody').append(row);

          //<td>${data[i].trueRatingsCount}</td>
        }
      }
    },
    error: function (data) {
      alert(data);
    },
  });
}

function DownloadInflection(langId, langTitle) {
  $('.loading').show();
  $.ajax({
    url: '/Cell/downloadUnimorph',
    type: 'POST',
    data: {
      LangId: langId,
    },
    success: function (data) {
      $('.loading').hide();
      let file = ``;
      for (let i = 0; i < data.length; i++) {
        let tags = data[i].stags;
        if (data[i].atags) {
          tags += ';' + data[i].atags;
        }
        file += `${data[i].lemma}\t${data[i].form}\t${UM_Sort(tags)}\n`;
      }
      var blob = new Blob([file], { type: 'text/plain' });
      var link = document.createElement('a');
      link.href = URL.createObjectURL(blob);
      link.download = langTitle + 'unimorph.tsv';
      link.click();
    },
    error: function (data) {
      alert(data);
    },
  });
}

function DownloadFull(langId, langTitle) {
  $('.loading').show();
  $.ajax({
    url: '/Cell/downloadFull',
    type: 'POST',
    data: {
      LangId: langId,
    },
    success: function (data) {
      $('.loading').hide();
      let forms = data.forms;
      let lemmas = data.lemmas;

      let file = ``;
      // c.lemmaid, c.submitted AS form, c.slotid AS stags, c.agreementid AS atags, COUNT(r.cellid) AS trueratingscount
      // l.id, l.entry, pc.title AS pcalss, l.engmeaning AS meaning, l.stem1, l.stem2, l.stem3, l.stem4
      file += `Lemma\tForm\tTags\tClass\tStem1\tStem2\tStem3\tStem4\n`;
      for (let i = 0; i < forms.length; i++) {
        let f = forms[i];
        let l = lemmas.find((x) => x.id == f.lemma);
        console.log(l);
        let tags = f.stags;
        if (f.atags) {
          tags += ';' + f.atags;
        }
        file += `${l.entry}\t${f.form}\t${tags}\t`;
        file += `${l.pcalss}\t${l.stem1 ?? ''}\t`;
        file += `${l.stem2 ?? ''}\t${l.stem3 ?? ''}\t${l.stem4 ?? ''}\n`;
      }
      var blob = new Blob([file], { type: 'text/plain' });
      var link = document.createElement('a');
      link.href = URL.createObjectURL(blob);
      link.download = langTitle + '_Full.tsv';
      link.click();
    },
    error: function (data) {
      alert(data);
    },
  });
}

function DownloadLexicon(langId, langTitle) {
  $('.loading').show();
  $.ajax({
    url: '/Lemma/downloadFull',
    type: 'POST',
    data: {
      langid: langId,
    },
    success: function (data) {
      $('.loading').hide();
      let lemmas = data;

      let file = '';
      file += `Lemma\tClass\tTranslation\tStem1\tStem2\tStem3\tStem4\n`;
      for (let i = 0; i < lemmas.length; i++) {
        let l = lemmas[i];
        file += `${l.lemma}\t${l.classtitle}\t${l.meaning}\t${l.stem1 ?? ''}\t`;
        file += `${l.stem2 ?? ''}\t${l.stem3 ?? ''}\t${l.stem4 ?? ''}\n`;
      }
      var blob = new Blob([file], { type: 'text/plain' });
      var link = document.createElement('a');
      link.href = URL.createObjectURL(blob);
      link.download = langTitle + '_lexicon.tsv';
      link.click();
    },
    error: function (data) {
      alert(data);
    },
  });
}

$(document).ready(function () {
  getLangList();
  $('.downloadForms').each(function () {
    var langId = $(this).data('langid');
    var langTitle = $(this).data('langtitle');

    $(this).append(
      `<span onclick="DownloadInflection(${langId}, '${langTitle}')"><span class="material-icons">download</span></span>`
    );
    $(this).append(
      `<span onclick="DownloadFull(${langId}, '${langTitle}')"><span class="material-icons">archive</span></span>`
    );
  });

  $('.downloadLemmas').each(function () {
    var langId = $(this).data('langid');
    var langTitle = $(this).data('langtitle');
    $(this).append(
      `<span onclick="DownloadLexicon(${langId}, '${langTitle}')"><span class="material-icons">download</span></span>`
    );
  });
});
