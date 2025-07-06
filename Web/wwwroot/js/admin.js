let langs = [];
function getLangs() {
  $('.loading').show();
  $.ajax({
    url: '/Lang/list',
    type: 'GET',
    success: function (data) {
      langs = data.sort((a, b) => a.title.localeCompare(b.title));
      var options = [];
      let html = '';
      for (let i = 0; i < langs.length; i++) {
        options.push({ id: langs[i].id, text: langs[i].title });
        html += `<li class="lang" onclick="selectLang(${langs[i].id})">
          <code>${langs[i].code}</code> ${langs[i].title}
          </li>`;
      }
      $('#lstLangs').html(html);
      $('#cmbLangs').select2({ data: options });
      $('#cmbFormsEntryLang').select2({ data: options });
      $('#cmbFormsCheckLang').select2({ data: options });
      $('.loading').hide();
    },
    error: function (data) {
      console.log(data);
    },
  });
}
$('#btnAddLang').click(function () {
  $('#frmLang').show();
  $('#txtLangTitle').val('');
  $('#txtLangCode').val('');
  $('#txtLangDescription').val('');
  $('#txtLangValidChars').val('');
  $('#txtLatitude').val('');
  $('#txtLongitude').val('');
  $('#LangId').val('');
  $('#btnLangSubmit').html('Add');
});
function selectLang(id) {
  $('#btnLangSubmit').html('Save');
  $('#LangId').val(id);
  $.ajax({
    url: '/Lang/get',
    type: 'GET',
    data: {
      id: id,
    },
    success: function (data) {
      $('#frmLang').show();
      $('#txtLangTitle').val(data.title);
      $('#txtLangCode').val(data.code);
      $('#txtLangDescription').val(data.description);
      $('#txtLangValidChars').val(data.validchars);
      $('#txtLatitude').val(data.latitude);
      $('#txtLongitude').val(data.longitude);
    },
    error: function (data) {
      console.log(data);
    },
  });
}
function LangSubmit() {
  if ($('#btnLangSubmit').html() == 'Add') {
    insertLang();
  } else {
    updateLang();
  }
}
function insertLang() {
  $.ajax({
    url: '/Lang/insert',
    type: 'POST',
    data: {
      title: $('#txtLangTitle').val().trim(),
      code: $('#txtLangCode').val().trim(),
      description: $('#txtLangDescription').val().trim(),
      validchars: $('#txtLangValidChars').val().trim(),
      latitude: $('#txtLatitude').val().trim(),
      longitude: $('#txtLongitude').val().trim(),
    },
    success: function (data) {
      $('#frmLang').hide();
      getLangs();
    },
    error: function (data) {
      alert('Error');
    },
  });
}
function updateLang() {
  $.ajax({
    url: '/Lang/update',
    type: 'POST',
    data: {
      Id: $('#LangId').val(),
      Title: $('#txtLangTitle').val().trim(),
      Code: $('#txtLangCode').val().trim(),
      Description: $('#txtLangDescription').val().trim(),
      ValidChars: $('#txtLangValidChars').val().trim(),
      Latitude: $('#txtLatitude').val().trim(),
      Longitude: $('#txtLongitude').val().trim(),
    },
    success: function (data) {
      $('#frmLang').hide();
      getLangs();
    },
    error: function (data) {
      alert('Error');
    },
  });
}

$('#btnLangCancel').click(function () {
  $('#frmLang').hide();
});

$('#closePopup').click(function () {
  $('#popup').fadeOut();
});

// ========== users
$('#btnInviteUser').click(function () {
  $('#frmInviteUser').show();
  $('#txtUserEmail').val('');
  $('#btnUserSubmit').html('Invite');
});
$('#btnInviteUserCancel').click(function () {
  $('#frmInviteUser').hide();
});

function InviteUser() {
  $.ajax({
    url: '/User/invite',
    type: 'POST',
    data: {
      Username: $('#txtUserEmail').val().trim(),
      Role: $('#cmbUserRole').val(),
    },
    success: function (data) {
      alert('Invitation sent!');
      $('#frmInviteUser').hide();
    },
    error: function (data) {
      alert('Error');
    },
  });
}
let roles = ['Admin', 'Linguist', 'Speaker', 'Viewer', 'Pending'];
function ListUsers() {
  $('.loading').show();
  $.ajax({
    url: '/User/list',
    type: 'GET',
    success: function (data) {
      $('.loading').hide();
      let users = data;
      let html = ``;
      for (let i = 0; i < users.length; i++) {
        let role = roles[users[i].role];
        if (role == 'Pending') {
          role += ':' + roles[users[i].desiredRole];
        }
        let name = users[i].name ? ` (${users[i].name})` : '';

        html += `
        <li class="userItem" onclick="ChangeRole('${users[i].username}', '${role}', ${users[i].id})"> ${users[i].username}${name}
          <small>${role}</small>
        </li>`;
      }
      $('#lstUsers').html(html);
      $('.loading').hide();
    },
    error: function (data) {
      console.log(data);
    },
  });
}

function ChangeRole(userEmail, currentRole, id) {
  $('#frmChangeRole').show();
  $('#userIdToChange').val(id);
  $('#userEmail').html(userEmail);
  $('#currentRole').html(currentRole);
}

$('#btnChangeRoleSubmit').click(function () {
  $.ajax({
    url: '/User/changeRole',
    type: 'POST',
    data: {
      usrId: $('#userIdToChange').val(),
      newRole: $('#cmbUserRoleToChange').val(),
    },
    success: function (data) {
      alert('Role changed successfully!');
      $('#frmChangeRole').hide();
      ListUsers();
    },
    error: function (data) {
      alert('Error');
    },
  });
});

$('#btnChangeRoleCancel').click(function () {
  $('#frmChangeRole').hide();
});

// ==========
$(document).ready(function () {
  $('#frmLang').hide();
  $('#frmInviteUser').hide();
  $('#frmChangeRole').hide();
  getLangs();
  ListUsers();
  $('#popup').hide();
  $('.accordion-header').append('<span class="arrow">▼</span>');
  $('.accordion-header').click(function () {
    $(this).toggleClass('active');
    $(this).next().slideToggle();
  });
  $('.accordion-header').toggleClass('active');
  $('.accordion-content').show();
  $('#drawer-admin').addClass('active');
});
