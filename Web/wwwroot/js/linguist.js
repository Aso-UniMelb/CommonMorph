// ========= Custom Sort
const customOrder =
  'AaBbCcÇçDdEeÊêFfGgHhIiÎîJjKkLlMmNnOoPpQqRrSsŞşTtUuÛûVvWwXxYyZz';
const getCustomIndex = (char) => customOrder.indexOf(char);
function CustomSort(list) {
  list.sort((a, b) => {
    let textA = a.title.toLowerCase();
    let textB = b.title.toLowerCase();
    for (let i = 0; i < Math.min(textA.length, textB.length); i++) {
      const indexA = getCustomIndex(textA[i]);
      const indexB = getCustomIndex(textB[i]);
      if (indexA !== indexB) {
        return indexA - indexB;
      }
    }
    return textA.length - textB.length;
  });
  return list;
}
// =========
function validateInput(selector, chars) {
  $(selector).off();
  $(selector).on('input', function () {
    var inputVal = $(this).val();
    var validchars = new RegExp(`^[ ${chars}]*$`);
    if (!validchars.test(inputVal)) {
      $(this).val(inputVal.replace(new RegExp(`[^ ${chars}]`, 'g'), ''));
    }
  });
}
// =========
$('#closePopup').click(function () {
  $('#popup').fadeOut();
});

$(document).ready(function () {
  $('#drawer-Linguist').addClass('active');
  $('.accordion-header').append('<span class="arrow">▼</span>');
  $('.accordion-header').click(function () {
    $(this).toggleClass('active');
    $(this).next().slideToggle();
  });
  $('.accordion-header').toggleClass('active');
  $('.accordion-content').show();

  $('.tutor').click(function () {
    $('.accordion-header').toggleClass('active');
    $('.accordion-content').show();
    introJs()
      .setOptions({
        steps: [
          {
            title: 'Welcome to the Linguist Panel!',
            intro:
              "I'll guide you through the process of contributing to the CommonMorph project. Please click the 'Next' button below to begin.",
          },
          {
            element: document.querySelector('#agreement-accordion'),
            intro: `This is the agreement group manager. It helps you input agreement morphems.
            Note: You can skip it if your language does not have a complex agreement system.`,
          },
          {
            element: document.querySelector('#paradigm-accordion'),
            intro:
              'This is the paradigm class and slot manager. It helps you input each slot in the paradigm.',
          },
          {
            element: document.querySelector('#lemma-accordion'),
            intro:
              'This is the lexicon. It helps you input each lemma and its paradigm class.',
          },
        ],
      })
      .start();
  });
});
