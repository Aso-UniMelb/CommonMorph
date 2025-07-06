$(document).ready(function () {
  $('.toggle-drawer').click(function () {
    $('#drawer-panel').addClass('open');
    $('#overlay').fadeIn(300);
  });

  $('#overlay').click(function () {
    $('#drawer-panel').removeClass('open');
    $('#overlay').fadeOut(300);
  });
});
