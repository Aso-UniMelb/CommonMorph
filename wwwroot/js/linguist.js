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
            element: document.querySelector('#affix-accordion'),
            intro: `This is the reusable layer manager. It helps you input reusable morphems.
            Note: You can skip it if your language does not have a complex affix system.`,
          },
          {
            element: document.querySelector('#inflection-classes-accordion'),
            intro:
              'This is the paradigm structure manager. It helps you input different structures for the inflection classes.',
          },
          {
            element: document.querySelector('#lemma-accordion'),
            intro:
              'This is the lexicon. It helps you input each lemma and its inflection class.',
          },
        ],
      })
      .start();
  });
});
