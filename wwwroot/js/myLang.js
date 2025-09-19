var myLang;
var myMetalang;
var myLevel;

function getMyLang() {
  let myLangVar = localStorage.getItem('myLang');
  if (myLangVar) {
    myLang = JSON.parse(myLangVar);
    $('#myLang').html(myLang.title);
  } else {
    alert('Please select your lnaguage variety first');
    window.location.href = '/app/dashboard';
  }

  let myMetalanguage = localStorage.getItem('myMetalang');
  if (myMetalanguage) {
    myMetalang = myMetalanguage;
  } else {
    alert('Please select your meta-language first');
    window.location.href = '/app/dashboard';
  }

  let myGrammarLevel = localStorage.getItem('myLevel');
  if (myGrammarLevel) {
    myLevel = myGrammarLevel;
  } else {
    alert('Please select your grammar knowledge level first');
    window.location.href = '/app/dashboard';
  }
}

$(document).ready(function () {
  getMyLang();
});
