let langs = [];

function getLangs() {
  $('.loading').show();
  $.ajax({
    url: '/Lang/list',
    type: 'GET',
    success: function (data) {
      $('.loading').hide();
      langs = data.sort((a, b) => a.title.localeCompare(b.title));
      var options = [];
      for (let i = 0; i < langs.length; i++) {
        options.push({
          id: langs[i].id,
          text: `[${langs[i].code}] ${langs[i].title}`,
        });
      }
      $('#cmbLangs').select2({ data: options });
      const myLangvar = localStorage.getItem('myLang');
      if (myLangvar) {
        $('#cmbLangs').val(JSON.parse(myLangvar).id).trigger('change');
      } else {
        localStorage.setItem('myLang', JSON.stringify(langs[0]));
      }
      getLangStats($('#cmbLangs').val());
    },
    error: function (data) {
      console.log(data);
    },
  });
}

function getLangStats(langid) {
  $('.loading').show();
  $('#langstats').html('');
  $.ajax({
    url: '/Elicit/GetStats',
    type: 'GET',
    data: {
      langid: langid,
    },
    success: function (data) {
      $('.loading').hide();
      if (data.countAll == 0) {
        $('#langstats').append(`<span>No linguistic data added!</div>`);
      } else {
        let progress = Math.ceil(
          ((data.countAll - data.remaining) * 100) / data.countAll
        );
        $('#langstats').append(
          `<div style="width:${progress}%"></div><span>${
            data.countAll - data.remaining
          } from ${data.countAll} (${progress}%)</span>`
        );
      }
    },
    error: function (data) {
      console.log(data);
    },
  });
  $.ajax({
    url: '/ActiveLearning/checkModelTrained',
    type: 'GET',
    data: {
      langid: langid,
    },
    success: function (data) {
      $('#modelTrainedTime').html(
        `<span style="color:green;">The model was last trained on: ${
          JSON.parse(data).message
        }</span>`
      );
    },
    error: function (data) {
      $('#modelTrainedTime').html(
        `<span style="color:red;">Not trained</span>`
      );
    },
  });
}
$('#btnTrainModel').click(function () {
  this.disabled = true;
  $.ajax({
    url: '/ActiveLearning/train',
    type: 'POST',
    data: {
      langid: $('#cmbLangs').val(),
    },
    success: function (data) {
      $('#modelTrainedTime').html(
        `<span style="color:green;">The model was trained</span>`
      );
      // refresh the page
      window.location.href = '/app/dashboard';
      this.disabled = false;
    },
    error: function (data) {
      $('#modelTrainedTime').html(
        `<span style="color:red;">Not trained</span>`
      );
    },
  });
  let progress = 0;
  const duration = 120000; // 2 minutes
  const intervalTime = 1000; // update every 1 second
  const increment = 100 / (duration / intervalTime);
  const interval = setInterval(() => {
    progress += increment;
    if (progress >= 100) {
      progress = 100;
      clearInterval(interval);
    }
    $('#modelTrainedTime').html(Math.floor(progress) + '%');
  }, intervalTime);
});

$('#cmbLangs').on('select2:select', function (e) {
  var selectedItem = e.params.data;
  let d = langs.findIndex(function (item, i) {
    return item.id == selectedItem.id;
  });
  localStorage.setItem('myLang', JSON.stringify(langs[d]));
  getLangStats(selectedItem.id);
});

$.each(i18n, function (key, value) {
  $('#cmbMetalang').append(
    `<option value="${key}">${i18n[key]['name']}</option>`
  );
});

$('#cmbLevel').append(`<option value="0">None</option>`);
$('#cmbLevel').append(`<option value="1">Basic (high school grammar)</option>`);
$('#cmbLevel').append(
  `<option value="2">Expert (familiar with linguistics terms)</option>`
);

// role change
$('#btnChangeRole').click(function () {
  $('#frmChangeRole').show();
});
$('#btnChangeRoleCancel').click(function () {
  $('#frmChangeRole').hide();
});
$('#btnChangeRoleSubmit').click(function () {
  $.ajax({
    url: '/User/changeMyRole',
    type: 'POST',
    data: {
      newRole: $('#cmbUserRoleToChange').val(),
    },
    success: function (data) {
      $('#frmChangeRole').hide();
      // refresh the page
      window.location.href = '/app/dashboard';
      // location.reload(true);
    },
    error: function (data) {
      alert('Error');
    },
  });
});

$('.tutor').click(function () {
  $('.accordion-header').toggleClass('active');
  $('.accordion-content').show();
  introJs()
    .setOptions({
      steps: [
        {
          title: 'Welcome to the CommonMorph',
          intro:
            "I'll guide you through the process of contributing to the CommonMorph project. Please click the 'Next' button below to begin.",
        },
        {
          element: document.querySelector('#btnChangeRole'),
          intro:
            'You can contribute as a Linguist or as a Speaker. Change your role in the CommonMorph project here.',
        },
        {
          element: document.querySelector('#cmbLangs'),
          intro: 'Change your language variety here.',
        },
        {
          element: document.querySelector('#cmbMetalang'),
          intro: 'change your meta-language here.',
        },
      ],
    })
    .start();
});

$(document).ready(function () {
  $('#drawer-dashboard').addClass('active');
  $('#frmChangeRole').hide();
  let myMetalang = localStorage.getItem('myMetalang');
  if (myMetalang) {
    $('#cmbMetalang').val(myMetalang).trigger('change');
  } else {
    localStorage.setItem('myMetalang', $('#cmbMetalang').val());
  }

  $('#cmbMetalang').change(function () {
    localStorage.setItem('myMetalang', $('#cmbMetalang').val());
  });

  let myLevel = localStorage.getItem('myLevel');
  if (myLevel) {
    $('#cmbLevel').val(myLevel).trigger('change');
  } else {
    localStorage.setItem('myLevel', $('#cmbLevel').val());
  }

  $('#cmbLevel').change(function () {
    localStorage.setItem('myLevel', $('#cmbLevel').val());
  });
  let myGrammarLevel = localStorage.getItem('myLevel');
  if (myGrammarLevel) {
    myLevel = myGrammarLevel;
  } else {
    alert('Please select your grammar knowledge level first');
    window.location.href = '/app/dashboard';
  }
  getLangs();
});
