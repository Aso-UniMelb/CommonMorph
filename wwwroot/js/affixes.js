// ========= ReusableLayers
ReusableLayers = [];

function getReusableLayers(langId) {
  $('.loading').show();
  $.ajax({
    url: '/Affix/listLayers',
    type: 'GET',
    data: {
      langId: langId,
    },
    success: function (data) {
      $('.loading').hide();
      ReusableLayers = data;
      let html = '';
      $('#cmbStructureReusableLayer').empty();
      $('#cmbStructureReusableLayer').append(`<option value="0">-</option>`);
      for (let i = 0; i < ReusableLayers.length; i++) {
        $('#cmbStructureReusableLayer').append(
          `<option value="${ReusableLayers[i].id}">${ReusableLayers[i].title}</option>`
        );
        html += `<li id="AG_${ReusableLayers[i].id}">
        <div class="list-title">
          <div id="AGT_${ReusableLayers[i].id}" class="reusableLayer">${ReusableLayers[i].title}</div>
          <div>
            <span class="smallButton AddAgreemnt" data-AG="${ReusableLayers[i].id}"><span class="material-icons">add_circle</span></span>
            <span class="smallButton" onclick="RenameReusableLayer(${ReusableLayers[i].id})"><span class="material-icons">edit</span></span>
          </div>
        </div>
        <ul class="affixes"></ul>
        </li>`;
      }
      $('#lstReusableLayers').html(html);
      $('.affixes').toggle();
    },
    error: function (data) {
      console.log(data);
    },
  });
}

//
$(document).ready(function () {
  $(document).on('click', '.reusableLayer', function (event) {
    event.stopPropagation();
    GetAffixes($(this).parent().parent());
  });
});

function GetAffixes(AG) {
  let ul = AG.children('ul');
  if (AG.attr('data-opened') != 'true') {
    $('.loading').show();
    $.ajax({
      url: '/Affix/listAffixes',
      type: 'GET',
      data: {
        reusablelayerid: AG.attr('id').replace('AG_', ''),
      },
      success: function (data) {
        $('.loading').hide();
        for (let i = 0; i < data.length; i++) {
          ul.append(`<li class="" onclick="EditAffix(${data[i].id})">
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
function EditAffix(id) {
  $('.loading').show();
  $.ajax({
    url: '/Affix/getAffix',
    type: 'GET',
    data: {
      id: id,
    },
    success: function (data) {
      $('#txtAffixUniMorphTags').val(data.unimorphtags);
      updateAggrementUMselects(data.unimorphtags.split(/[;+]/));
      $('#txtAffixTitle').val(data.title);
      $('#txtAffixRealization').val(data.realization);
      $('#AffixId').val(data.id);
      $('#txtAffixOrder').val(data.order);
      $('#AffixGroupId').val(data.reusablelayerid);
      $('#btnAffixSubmit').html('Save');
      $('#frmAffix').show();
      $('.loading').hide();
    },
    error: function (data) {
      console.log(data);
    },
  });
}
// add agreemnt items
$(document).on('click', '.AddAgreemnt', function (event) {
  $('#frmAffix').show();
  $('#txtAffixUniMorphTags').val('');
  $('#txtAffixTitle').val('');
  $('#txtAffixRealization').val('');
  $('#AffixId').val('');
  $('#txtAffixOrder').val('1');
  $('#btnAffixSubmit').html('Add');
  $('#AffixGroupId').val($(this).attr('data-AG'));
  updateAggrementUMselects([]);
});

$('#btnAffixCancel').click(function () {
  $('#frmAffix').hide();
});

function AffixSubmit() {
  if ($('#btnAffixSubmit').html() == 'Add') {
    insertAffix();
  } else {
    updateAffix();
  }
}

function insertAffix() {
  $('.loading').show();
  let AG = $('#AffixGroupId').val();
  let realization = $('#txtAffixRealization').val().trim();
  let title = $('#txtAffixTitle').val().trim();
  let order = $('#txtAffixOrder').val();
  $.ajax({
    url: '/Affix/insertAffix',
    type: 'POST',
    data: {
      unimorphtags: UM_Sort($('#txtAffixUniMorphTags').val().trim()),
      title: title,
      realization: realization,
      order: order,
      reusablelayerid: AG,
    },
    success: function (data) {
      $('.loading').hide();
      $('#frmAffix').hide();
      let sublist = $('#AG_' + AG).children('ul');
      let newAffix = $(`<li class="" id="A_${data}">
        <span>${title} <small>(${realization})</small></span>
        </li>`);
      sublist.append(newAffix);
    },
    error: function (data) {
      alert('Error');
    },
  });
}

function updateAffix() {
  $('.loading').show();
  let AG = $('#AffixGroupId').val();
  let realization = $('#txtAffixRealization').val().trim();
  let title = $('#txtAffixTitle').val().trim();
  let order = $('#txtAffixOrder').val();
  $.ajax({
    url: '/Affix/updateAffix',
    type: 'POST',
    data: {
      id: $('#AffixId').val(),
      unimorphtags: UM_Sort($('#txtAffixUniMorphTags').val().trim()),
      title: title,
      realization: realization,
      order: order,
      reusablelayerid: AG,
    },
    success: function (data) {
      $('.loading').hide();
      $('#frmAffix').hide();
      let e = $('#AG_' + AG);
      e.children('ul').children('li').remove();
      e.attr('data-opened', 'false');
      GetAffixes(e);
    },
    error: function (data) {
      alert('Error');
    },
  });
}

// ======= Add/rename reusable layers
function ReusableLayerSubmit() {
  if ($('#btnReusableLayerSubmit').html() == 'Add') {
    insertReusableLayer();
  } else {
    updateReusableLayer();
  }
}

$('#btnReusableLayerCancel').click(function () {
  $('#frmReusableLayer').hide();
});

// add reusable layer
$('#btnAddReusableLayer').click(function () {
  $('#frmReusableLayer').show();
  $('#ReusableLayerId').val('');
  $('#txtReusableLayerTitle').val('');
  $('#btnReusableLayerSubmit').html('Add');
});

function insertReusableLayer() {
  $('.loading').show();
  let title = $('#txtReusableLayerTitle').val().trim();
  $.ajax({
    url: '/Affix/insertLayer',
    type: 'POST',
    data: {
      title: title,
      langid: myLang.id,
    },
    success: function (data) {
      ReusableLayers.push({ Id: data, Title: title });
      $('#frmReusableLayer').hide();
      $('#cmbStructureReusableLayer').append(
        `<option value="${data}">${title}</option>`
      );
      $('#lstReusableLayers').append(`<li id="AG_${data}">
        <div class="list-title">
          <div id="AGT_${data}" class="reusableLayer">${title}</div>
          <div>
            <span class="smallButton AddAgreemnt" data-AG="${data}"><span class="material-icons">add_circle</span></span>
            <span class="smallButton" onclick="RenameReusableLayer(${data})"><span class="material-icons">edit</span></span>
          </div>
        </div>
        <ul class="affixes"></ul>
        </li>`);
      $('.loading').hide();
    },
    error: function (data) {
      alert('Error');
    },
  });
}

// rename reusable layer
function RenameReusableLayer(id) {
  $('#frmReusableLayer').show();
  $('#btnReusableLayerSubmit').html('Save');
  $('#txtReusableLayerTitle').val(
    ReusableLayers.find((x) => x.id == id).title
  );
  $('#ReusableLayerId').val(id);
}

function updateReusableLayer() {
  let id = $('#ReusableLayerId').val();
  let title = $('#txtReusableLayerTitle').val().trim();
  $('.loading').show();
  $.ajax({
    url: '/Affix/updateLayer',
    type: 'POST',
    data: {
      id: id,
      title: title,
      langid: myLang.id,
    },
    success: function (data) {
      $('.loading').hide();
      ReusableLayers.find((x) => x.id == id).title = title;
      $('#AGT_' + id).html(title);
      $('#frmReusableLayer').hide();
    },
    error: function (data) {
      alert('Error');
    },
  });
}

// UMtagSelector
$('#AffixUMtagSelector').append(
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
  $('#txtAffixUniMorphTags').val(UM_Sort(tagset.join(';')));
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

// ========= import affixes
// show import form
$('#btnImportAffixes').click(function () {
  $('#frmImportAffix').show();
});
// cancel import form
$('#btnAffixImportCancel').click(function () {
  $('#frmImportAffix').hide();
});
// submit import form
function AffixImportSubmit() {
  // disable the button
  $('#btnAffixImportSubmit').attr('disabled', true);
  $('.loading').show();
  $.ajax({
    url: '/Affix/import',
    type: 'POST',
    data: {
      file: $('#txtAffixImport').val(),
      langid: myLang.id,
    },
    success: function (data) {
      $('.loading').hide();
      $('#frmImportAffix').hide();
      $('#btnAffixImportSubmit').attr('disabled', false);
      getReusableLayers(myLang.id);
    },
    error: function (data) {
      alert('Error');
    },
  });
}

// export Affixes
$('#btnExportAffixes').click(async function () {
  let file = '';
  for (const ag of ReusableLayers) {
    file += '#' + ag.title + '\n';
    try {
      const response = await fetch(
        '/Affix/listAffixes?' +
          new URLSearchParams({
            ReusableLayerId: ag.id,
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
      console.error('Error fetching structure list:', error);
    }
  }
  const blob = new Blob([file], { type: 'text/plain' });
  const link = document.createElement('a');
  link.href = URL.createObjectURL(blob);
  link.download = myLang.title + '_Affixes.tsv';
  link.click();
});

$(document).ready(function () {
  getReusableLayers(myLang.id);
});
