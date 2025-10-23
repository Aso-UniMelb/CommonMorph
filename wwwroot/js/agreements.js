// ========= Agreement Groups
AgreementGroups = [];

function getAgreementGroups(langId) {
  $('.loading').show();
  $.ajax({
    url: '/Agreement/listGroups',
    type: 'GET',
    data: {
      langId: langId,
    },
    success: function (data) {
      $('.loading').hide();
      AgreementGroups = data;
      let html = '';
      $('#cmbSlotAgreementGroup').empty();
      $('#cmbSlotAgreementGroup').append(`<option value="0">-</option>`);
      for (let i = 0; i < AgreementGroups.length; i++) {
        $('#cmbSlotAgreementGroup').append(
          `<option value="${AgreementGroups[i].id}">${AgreementGroups[i].title}</option>`
        );
        html += `<li id="AG_${AgreementGroups[i].id}">
        <div class="list-title">
          <div id="AGT_${AgreementGroups[i].id}" class="agreementGroup">${AgreementGroups[i].title}</div>
          <div>
            <span class="smallButton AddAgreemnt" data-AG="${AgreementGroups[i].id}"><span class="material-icons">add_circle</span></span>
            <span class="smallButton" onclick="RenameAgreementGroup(${AgreementGroups[i].id})"><span class="material-icons">edit</span></span>
          </div>
        </div>
        <ul class="agreementItems"></ul>
        </li>`;
      }
      $('#lstAgreementGroups').html(html);
      $('.agreementItems').toggle();
    },
    error: function (data) {
      console.log(data);
    },
  });
}

//
$(document).ready(function () {
  $(document).on('click', '.agreementGroup', function (event) {
    event.stopPropagation();
    GetItems($(this).parent().parent());
  });
});

function GetItems(AG) {
  let ul = AG.children('ul');
  if (AG.attr('data-opened') != 'true') {
    $('.loading').show();
    $.ajax({
      url: '/Agreement/listItems',
      type: 'GET',
      data: {
        agreementgroupId: AG.attr('id').replace('AG_', ''),
      },
      success: function (data) {
        $('.loading').hide();
        for (let i = 0; i < data.length; i++) {
          ul.append(`<li class="" onclick="EditAgreementItem(${data[i].id})">
          <span class="priority${data[i].priority}"></span>
          ${data[i].realization} <small>(${data[i].title})</small>
          </li>`);
        }
      },
      error: function (data) {
        console.log(data);
      },
    });
    AG.attr('data-opened', 'true');
  }
  ul.toggle();
  AG.toggleClass('opened');
}
//
function EditAgreementItem(id) {
  $('.loading').show();
  $.ajax({
    url: '/Agreement/getItem',
    type: 'GET',
    data: {
      id: id,
    },
    success: function (data) {
      $('#txtAgreementItemUniMorphTags').val(data.unimorphtags);
      updateAggrementUMselects(data.unimorphtags.split(/[;+]/));
      $('#txtAgreementItemTitle').val(data.title);
      $('#txtAgreementItemRealization').val(data.realization);
      $('#AgreementItemId').val(data.id);
      $('#txtAgreementItemOrder').val(data.order);
      $('#AgreementItemGroupId').val(data.agreementgroupid);
      $('#btnAgreementItemSubmit').html('Save');
      $('#frmAgreementItem').show();
      $('.loading').hide();
    },
    error: function (data) {
      console.log(data);
    },
  });
}
// add agreemnt items
$(document).on('click', '.AddAgreemnt', function (event) {
  $('#frmAgreementItem').show();
  $('#txtAgreementItemUniMorphTags').val('');
  $('#txtAgreementItemTitle').val('');
  $('#txtAgreementItemRealization').val('');
  $('#AgreementItemId').val('');
  $('#txtAgreementItemOrder').val('1');
  $('#btnAgreementItemSubmit').html('Add');
  $('#AgreementItemGroupId').val($(this).attr('data-AG'));
  updateAggrementUMselects([]);
});

$('#btnAgreementItemCancel').click(function () {
  $('#frmAgreementItem').hide();
});

function AgreementItemSubmit() {
  if ($('#btnAgreementItemSubmit').html() == 'Add') {
    insertAgreementItem();
  } else {
    updateAgreementItem();
  }
}

function insertAgreementItem() {
  $('.loading').show();
  let AG = $('#AgreementItemGroupId').val();
  let realization = $('#txtAgreementItemRealization').val().trim();
  let title = $('#txtAgreementItemTitle').val().trim();
  let priority = $('#txtAgreementItemOrder').val();
  $.ajax({
    url: '/Agreement/insertItem',
    type: 'POST',
    data: {
      unimorphtags: UM_Sort($('#txtAgreementItemUniMorphTags').val().trim()),
      title: title,
      realization: realization,
      priority: priority,
      agreementgroupid: AG,
    },
    success: function (data) {
      $('.loading').hide();
      $('#frmAgreementItem').hide();
      let sublist = $('#AG_' + AG).children('ul');
      let newItem = $(`<li class="" id="A_${data}">
        <span class="priority${priority}"></span>
        <span>${title} <small>(${realization})</small></span>
        </li>`);
      sublist.append(newItem);
    },
    error: function (data) {
      alert('Error');
    },
  });
}

function updateAgreementItem() {
  $('.loading').show();
  let AG = $('#AgreementItemGroupId').val();
  let realization = $('#txtAgreementItemRealization').val().trim();
  let title = $('#txtAgreementItemTitle').val().trim();
  let priority = $('#txtAgreementItemOrder').val();
  $.ajax({
    url: '/Agreement/updateItem',
    type: 'POST',
    data: {
      id: $('#AgreementItemId').val(),
      unimorphtags: UM_Sort($('#txtAgreementItemUniMorphTags').val().trim()),
      title: title,
      realization: realization,
      priority: priority,
      agreementgroupid: AG,
    },
    success: function (data) {
      $('.loading').hide();
      $('#frmAgreementItem').hide();
      let e = $('#AG_' + AG);
      e.children('ul').children('li').remove();
      e.attr('data-opened', 'false');
      GetItems(e);
    },
    error: function (data) {
      alert('Error');
    },
  });
}

// ======= Add/rename agreement groups
function AgreementGroupSubmit() {
  if ($('#btnAgreementGroupSubmit').html() == 'Add') {
    insertAgreementGroup();
  } else {
    updateAgreementGroup();
  }
}

$('#btnAgreementGroupCancel').click(function () {
  $('#frmAgreementGroup').hide();
});

// add agreement group
$('#btnAddAgreementGroup').click(function () {
  $('#frmAgreementGroup').show();
  $('#AgreementGroupId').val('');
  $('#txtAgreementGroupTitle').val('');
  $('#btnAgreementGroupSubmit').html('Add');
});

function insertAgreementGroup() {
  $('.loading').show();
  let title = $('#txtAgreementGroupTitle').val().trim();
  $.ajax({
    url: '/Agreement/insertGroup',
    type: 'POST',
    data: {
      title: title,
      langid: myLang.id,
    },
    success: function (data) {
      AgreementGroups.push({ Id: data, Title: title });
      $('#frmAgreementGroup').hide();
      $('#cmbSlotAgreementGroup').append(
        `<option value="${data}">${title}</option>`
      );
      $('#lstAgreementGroups').append(`<li id="AG_${data}">
        <div class="list-title">
          <div id="AGT_${data}" class="agreementGroup">${title}</div>
          <div>
            <span class="smallButton AddAgreemnt" data-AG="${data}"><span class="material-icons">add_circle</span></span>
            <span class="smallButton" onclick="RenameAgreementGroup(${data})"><span class="material-icons">edit</span></span>
          </div>
        </div>
        <ul class="agreementItems"></ul>
        </li>`);
      $('.loading').hide();
    },
    error: function (data) {
      alert('Error');
    },
  });
}

// rename agreement group
function RenameAgreementGroup(id) {
  $('#frmAgreementGroup').show();
  $('#btnAgreementGroupSubmit').html('Save');
  $('#txtAgreementGroupTitle').val(
    AgreementGroups.find((x) => x.id == id).title
  );
  $('#AgreementGroupId').val(id);
}

function updateAgreementGroup() {
  let id = $('#AgreementGroupId').val();
  let title = $('#txtAgreementGroupTitle').val().trim();
  $('.loading').show();
  $.ajax({
    url: '/Agreement/updateGroup',
    type: 'POST',
    data: {
      id: id,
      title: title,
      langid: myLang.id,
    },
    success: function (data) {
      $('.loading').hide();
      AgreementGroups.find((x) => x.id == id).title = title;
      $('#AGT_' + id).html(title);
      $('#frmAgreementGroup').hide();
    },
    error: function (data) {
      alert('Error');
    },
  });
}

// UMtagSelector
$('#AgreementUMtagSelector').append(
  `<div class="UMtagSelector"><div><i>Add feature:</i></div>
    <label>Dimension:</label>
    <select id="dimensionA"></select>
    <br />
    <label>Feature:</label>
    <select id="featureA"></select>
    <button type="button" id="btnAddUMtagA">⇧ Add</button>
  </div>
  
  `
);
AggrementDimensions.forEach((dim) => {
  $('#dimensionA').append(`<option value="${dim}">${dim}</option>`);
});

$('#dimensionA').change(function () {
  $('#featureA').html(' ');
  UM.forEach((tag) => {
    let dim = tag.d;
    if (dim == $('#dimensionA').val()) {
      let feat = tag.f;
      $('#featureA').append(
        `<option value="${tag.l}" title="${feat}">${feat}</option>`
      );
    }
  });
});
$('#dimensionA').change();

$('#btnAddUMtagA').click(function () {
  let dim = $('#dimensionA').val();
  let feat = $('#featureA').val();
  let feat_title = $('#featureA  option:selected').attr('title');
  if ($('#F_' + feat).length == 0 && dim && feat) {
    $('#addedUMtagsA').append(
      `<div class="UMtag" id="F_${feat}"><span class="remove" onclick="RemoveFeatA('${feat}')">&times;</span> ${dim}: ${feat_title}</div>`
    );
  }
  UpdateUniMorphTagSetA();
});

function RemoveFeatA(feat) {
  $('#F_' + feat).remove();
  UpdateUniMorphTagSetA();
}

function UpdateUniMorphTagSetA() {
  tagset = [];
  let added = $('#addedUMtagsA').children();
  console.log(added);
  for (let i = 0; i < added.length; i++) {
    console.log(added[i]);
    let feat = $(added[i]).attr('id').replace('F_', '');
    tagset.push(feat);
  }
  $('#txtAgreementItemUniMorphTags').val(UM_Sort(tagset.join(';')));
}

function updateAggrementUMselects(tagset) {
  $('#addedUMtagsA').html('');
  tagset.forEach((feat) => {
    if (feat) {
      let vec = UM.find((item) => item.l === feat);
      $('#addedUMtagsA').append(
        `<div class="UMtag" id="F_${feat}"><span class="remove" onclick="RemoveFeatA('${feat}')">&times;</span> ${vec.d}: ${vec.f}</div>`
      );
    }
  });
}

// ========= import agreements
// show import form
$('#btnImportAgreements').click(function () {
  $('#frmImportAgreement').show();
});
// cancel import form
$('#btnAgreementImportCancel').click(function () {
  $('#frmImportAgreement').hide();
});
// submit import form
function AgreementImportSubmit() {
  // disable the button
  $('#btnAgreementImportSubmit').attr('disabled', true);
  $('.loading').show();
  $.ajax({
    url: '/Agreement/import',
    type: 'POST',
    data: {
      file: $('#txtAgreementImport').val(),
      langid: myLang.id,
    },
    success: function (data) {
      $('.loading').hide();
      $('#frmImportAgreement').hide();
      $('#btnAgreementImportSubmit').attr('disabled', false);
      getAgreementGroups(myLang.id);
    },
    error: function (data) {
      alert('Error');
    },
  });
}

// export Agreements
$('#btnExportAgreements').click(async function () {
  let file = '';
  for (const ag of AgreementGroups) {
    file += '#' + ag.title + '\n';
    try {
      const response = await fetch(
        '/Agreement/listItems?' +
          new URLSearchParams({
            AgreementGroupId: ag.id,
          })
      );
      if (!response.ok) {
        throw new Error(response.status);
      }
      const data = await response.json();
      file +=
        data
          .map((s) =>
            [s.order, s.unimorphtags, s.realization, s.title].join('\t')
          )
          .join('\n') + '\n';
    } catch (error) {
      console.error('Error fetching slot list:', error);
    }
  }
  const blob = new Blob([file], { type: 'text/plain' });
  const link = document.createElement('a');
  link.href = URL.createObjectURL(blob);
  link.download = myLang.title + '_Agreements.tsv';
  link.click();
});

$(document).ready(function () {
  getAgreementGroups(myLang.id);
});
