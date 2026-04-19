// ========= InflectionClasses
InflectionClasses = [];

function getInflectionClasses(langid) {
  $('.loading').show();
  $.ajax({
    url: '/InflectionClass/list',
    type: 'GET',
    data: {
      langid: langid,
    },
    success: function (data) {
      $('.loading').hide();
      InflectionClasses = data;
      var options = [];
      let html = '';
      for (let i = 0; i < InflectionClasses.length; i++) {
        options.push({
          id: InflectionClasses[i].id,
          text: InflectionClasses[i].title,
        });
        html += `<li id="PC_${InflectionClasses[i].id}">
        <div class="list-title">
          <div id="PCT_${InflectionClasses[i].id}" class="inflectionClass">${InflectionClasses[i].title}</div>
          <div>
            <span class="smallButton AddStructure" data-PC="${InflectionClasses[i].id}"><span class="material-icons">add_circle</span></span>
            <span class="smallButton" onclick="EditInflectionClass(${InflectionClasses[i].id})"><span class="material-icons">edit</span></span>        
          </div>
        </div>
        <ul class="structures"></ul>
        </li>`;
      }
      $('#lstInflectionClasses').html(html);
      $('.structures').toggle();
      $('#cmbLemmaInflectionClass').select2({ data: options });
    },
    error: function (data) {
      console.log(data);
    },
  });
}
$(document).ready(function () {
  $(document).on('click', '.inflectionClass', function (event) {
    event.stopPropagation();
    GetStructures($(this).parent().parent());
  });
});

function GetStructures(PC) {
  let ul = PC.children('ul');
  if (PC.attr('data-opened') != 'true') {
    $('.loading').show();
    $.ajax({
      url: '/Structure/list',
      type: 'GET',
      data: {
        inflectionclassid: PC.attr('id').replace('PC_', ''),
      },
      success: function (data) {
        $('.loading').hide();
        for (let i = 0; i < data.length; i++) {
          ul.append(`<li class="" onclick="EditStructure(${data[i].id})">
          ${data[i].title}
          </li>`);
        }
      },
      error: function (data) {
        console.log(data);
      },
    });
    PC.attr('data-opened', 'true');
  }
  ul.toggle();
  PC.toggleClass('opened');
}
//
function EditStructure(id) {
  $('.loading').show();
  $.ajax({
    url: '/Structure/get',
    type: 'GET',
    data: {
      id: id,
    },
    success: function (data) {
      $('#txtStructureTitle').val(data.title);
      $('#txtStructureUniMorphTags').val(data.unimorphtags);
      updateStructureUMselects(data.unimorphtags.split(/[;+]/));
      $('#txtStructureFormula').val(data.formula);
      $('#StructureId').val(data.id);
      $('#cmbStructureReusableLayer').val(data.reusablelayerid).change();
      $('#btnStructuresSubmit').html('Save');
      $('#txtStructureOrder').val(data.order);
      $('#cmbStructureInflectionClass').val(data.inflectionclassid).trigger('change');
      $('#StructurePClassId').val(data.inflectionclassid);
      $('#frmStructures').show();
      $('.loading').hide();
    },
    error: function (data) {
      console.log(data);
    },
  });
}
//
$(document).on('click', '.AddStructure', function (event) {
  $('#txtStructureTitle').val('');
  $('#txtStructureUniMorphTags').val('');
  $('#txtStructureFormula').val('');
  $('#StructureId').val('');
  $('#cmbStructureReusableLayer').val('0');
  $('#btnStructuresSubmit').html('Add');
  $('#StructurePClassId').val($(this).attr('data-PC'));
  $('#frmStructures').show();
  updateStructureUMselects([]);
});

$('#btnStructuresCancel').click(function () {
  $('#frmStructures').hide();
});

function StructuresSubmit() {
  if ($('#btnStructuresSubmit').html() == 'Add') {
    insertStructure();
  } else {
    updateStructure();
  }
}

function insertStructure() {
  $('.loading').show();
  let PC = $('#StructurePClassId').val();
  let title = $('#txtStructureTitle').val().trim();
  let order = $('#txtStructureOrder').val();
  $.ajax({
    url: '/Structure/insert',
    type: 'POST',
    data: {
      title: title,
      unimorphtags: UM_Sort($('#txtStructureUniMorphTags').val().trim()),
      formula: $('#txtStructureFormula').val().trim(),
      reusablelayerid: $('#cmbStructureReusableLayer').val(),
      inflectionclassid: PC,
      order: order,
      inflectionclassid: PC,
    },
    success: function (data) {
      $('.loading').hide();
      $('#frmStructures').hide();
      let sublist = $('#PC_' + PC).children('ul');
      let newStructure = $(`<li class="" id="S_${data}">
        <span>${title}</span>
        </li>`);
      sublist.append(newStructure);
    },
    error: function (data) {
      alert('Error');
    },
  });
}

function updateStructure() {
  $('.loading').show();
  let PC = $('#StructurePClassId').val();
  let title = $('#txtStructureTitle').val().trim();
  let order = $('#txtStructureOrder').val();
  $.ajax({
    url: '/Structure/update',
    type: 'POST',
    data: {
      id: $('#StructureId').val(),
      title: title,
      unimorphtags: UM_Sort($('#txtStructureUniMorphTags').val().trim()),
      formula: $('#txtStructureFormula').val().trim(),
      reusablelayerid: $('#cmbStructureReusableLayer').val(),
      inflectionclassid: PC,
      order: order,
      inflectionclassid: PC,
    },
    success: function (data) {
      $('.loading').hide();
      $('#frmStructures').hide();
      let e = $('#PC_' + PC);
      e.children('ul').children('li').remove();
      e.attr('data-opened', 'false');
      GetStructures(e);
    },
    error: function (data) {
      alert('Error');
    },
  });
}

function InflectionClassSubmit() {
  if ($('#btnInflectionClassSubmit').html() == 'Add') {
    insertInflectionClass();
  } else {
    updateInflectionClass();
  }
}

$('#btnInflectionClassCancel').click(function () {
  $('#frmInflectionClass').hide();
});

$('#btnAddInflectionClass').click(function () {
  $('#frmInflectionClass').show();
  $('#txtInflectionClassTitle').val('');
  $('#txtInflectionClassDescription').val('');
  $('#InflectionClassId').val('');
  $('#btnInflectionClassSubmit').html('Add');
});

function insertInflectionClass() {
  $('.loading').show();
  let title = $('#txtInflectionClassTitle').val().trim();
  let description = $('#txtInflectionClassDescription').val().trim();
  $.ajax({
    url: '/InflectionClass/insert',
    type: 'POST',
    data: {
      title: title,
      langid: myLang.id,
      description: description,
    },
    success: function (data) {
      window.location.href = '/app/linguist'; // refresh the page
    },
    error: function (data) {
      alert('Error');
    },
  });
}

function EditInflectionClass(id) {
  $('#frmInflectionClass').show();
  $('#btnInflectionClassSubmit').html('Save');
  $('#txtInflectionClassTitle').val(
    InflectionClasses.find((x) => x.id == id).title
  );
  $('#txtInflectionClassDescription').val(
    InflectionClasses.find((x) => x.id == id).description
  );
  $('#InflectionClassId').val(id);
}

function updateInflectionClass() {
  let id = $('#InflectionClassId').val();
  let title = $('#txtInflectionClassTitle').val().trim();
  $('.loading').show();
  $.ajax({
    url: '/InflectionClass/update',
    type: 'POST',
    data: {
      id: id,
      title: title,
      langid: myLang.id,
      description: $('#txtInflectionClassDescription').val().trim(),
    },
    success: function (data) {
      $('.loading').hide();
      InflectionClasses.find((x) => x.id == id).title = title;
      $('#PCT_' + id).html(title);
      $('#frmInflectionClass').hide();
    },
    error: function (data) {
      alert('Error');
    },
  });
}

// UMtagSelector
$('#StructureUMtagSelector').append(
  `<div class="UMtagSelector"><div><i>Add feature:</i></div>
    <label>Dimension:</label>
    <select id="dimension"></select>
    <br />
    <label>Feature:</label>
    <select id="feature"></select>
    <button type="button" id="btnAddUMtag">⇧ Add</button>
  </div>
  `
);
Dimensions.forEach((dim) => {
  $('#dimension').append(`<option value="${dim}">${dim}</option>`);
});

$('#dimension').change(function () {
  $('#feature').html(' ');
  UM.forEach((tag) => {
    let dim = tag.d;
    if (dim == $('#dimension').val()) {
      let feat = tag.f;
      $('#feature').append(
        `<option value="${tag.l}" title="${feat}">${feat}</option>`
      );
    }
  });
});
$('#dimension').change();

$('#btnAddUMtag').click(function () {
  let dim = $('#dimension').val();
  let feat = $('#feature').val();
  let feat_title = $('#feature  option:selected').attr('title');
  if ($('#F_' + feat).length == 0 && dim && feat) {
    $('#addedUMtags').append(
      `<div class="UMtag" id="F_${feat}"><span class="remove" onclick="RemoveFeat('${feat}')">&times;</span> ${dim}: ${feat_title}</div>`
    );
  }
  UpdateUniMorphTagSet();
});

function RemoveFeat(feat) {
  $('#F_' + feat).remove();
  UpdateUniMorphTagSet();
}

function UpdateUniMorphTagSet() {
  tagset = [];
  let added = $('#addedUMtags').children();
  console.log(added);
  for (let i = 0; i < added.length; i++) {
    console.log(added[i]);
    let feat = $(added[i]).attr('id').replace('F_', '');
    tagset.push(feat);
  }
  $('#txtStructureUniMorphTags').val(UM_Sort(tagset.join(';')));
}

function updateStructureUMselects(tagset) {
  $('#addedUMtags').html('');
  console.log(tagset);
  tagset.forEach((feat) => {
    if (feat) {
      let vec = UM.find((item) => item.l === feat);
      $('#addedUMtags').append(
        `<div class="UMtag" id="F_${feat}"><span class="remove" onclick="RemoveFeat('${feat}')">&times;</span> ${vec.d}: ${vec.f}</div>`
      );
    }
  });
}
// ========= import Structures
// show import form
$('#btnImportStructure').click(function () {
  $('#frmImportStructures').show();
});
// cancel import form
$('#btnStructureImportCancel').click(function () {
  $('#frmImportStructures').hide();
});
// submit import form
function StructureImportSubmit() {
  $('.loading').show();
  $.ajax({
    url: '/Structure/import',
    type: 'POST',
    data: {
      file: $('#txtStructureImport').val(),
      langid: myLang.id,
    },
    success: function (data) {
      $('.loading').hide();
      $('#frmImportStructures').hide();
      getInflectionClasses(myLang.id);
    },
    error: function (data) {
      alert('Error');
    },
  });
}
// export structures
$('#btnExportStructure').click(async function () {
  let file = '';
  for (const pc of InflectionClasses) {
    file += '#' + pc.title + '\n';
    try {
      const response = await fetch(
        '/Structure/list?' +
          new URLSearchParams({
            InflectionClassID: pc.id,
          })
      );
      if (!response.ok) {
        throw new Error(response.status);
      }
      const data = await response.json();
      file +=
        data
          .map((s) => [s.unimorphtags, s.formula, s.title].join('\t'))
          .join('\n') + '\n';
    } catch (error) {
      console.error('Error fetching structure list:', error);
    }
  }
  const blob = new Blob([file], { type: 'text/plain' });
  const link = document.createElement('a');
  link.href = URL.createObjectURL(blob);
  link.download = myLang.title + '_Structures.tsv';
  link.click();
});

$(document).ready(function () {
  getInflectionClasses(myLang.id);
});
