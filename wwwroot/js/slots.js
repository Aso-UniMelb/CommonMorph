// ========= ParadigmClasses
ParadigmClasses = [];

function getParadigmClasses(langid) {
  $('.loading').show();
  $.ajax({
    url: '/ParadigmClass/list',
    type: 'GET',
    data: {
      langid: langid,
    },
    success: function (data) {
      $('.loading').hide();
      ParadigmClasses = data;
      var options = [];
      let html = '';
      for (let i = 0; i < ParadigmClasses.length; i++) {
        options.push({
          id: ParadigmClasses[i].id,
          text: ParadigmClasses[i].title,
        });
        html += `<li id="PC_${ParadigmClasses[i].id}">
        <div class="list-title">
          <div id="PCT_${ParadigmClasses[i].id}" class="paradigmClass">${ParadigmClasses[i].title}</div>
          <div>
            <span class="smallButton AddSlot" data-PC="${ParadigmClasses[i].id}"><span class="material-icons">add_circle</span></span>
            <span class="smallButton" onclick="EditParadigmClass(${ParadigmClasses[i].id})"><span class="material-icons">edit</span></span>        
          </div>
        </div>
        <ul class="slots"></ul>
        </li>`;
      }
      $('#lstParadigmClasses').html(html);
      $('.slots').toggle();
      $('#cmbLemmaParadigmClass').select2({ data: options });
    },
    error: function (data) {
      console.log(data);
    },
  });
}
$(document).ready(function () {
  $(document).on('click', '.paradigmClass', function (event) {
    event.stopPropagation();
    GetSlots($(this).parent().parent());
  });
});

function GetSlots(PC) {
  let ul = PC.children('ul');
  if (PC.attr('data-opened') != 'true') {
    $('.loading').show();
    $.ajax({
      url: '/Slot/list',
      type: 'GET',
      data: {
        paradigmclassid: PC.attr('id').replace('PC_', ''),
      },
      success: function (data) {
        $('.loading').hide();
        for (let i = 0; i < data.length; i++) {
          ul.append(`<li class="" onclick="EditSlot(${data[i].id})">
          <span class="priority${data[i].priority}"></span>
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
function EditSlot(id) {
  $('.loading').show();
  $.ajax({
    url: '/Slot/get',
    type: 'GET',
    data: {
      id: id,
    },
    success: function (data) {
      $('#txtSlotTitle').val(data.title);
      $('#txtSlotUniMorphTags').val(data.unimorphtags);
      updateSlotUMselects(data.unimorphtags.split(';'));
      $('#txtSlotFormula').val(data.formula);
      $('#SlotId').val(data.id);
      $('#cmbSlotAgreementGroup').val(data.agreementgroupid).change();
      $('#btnSlotsSubmit').html('Save');
      $('#cmbSlotPriority').val(data.priority);
      $('#cmbSlotParadigmClass').val(data.paradigmclassid).trigger('change');
      $('#SlotPClassId').val(data.paradigmclassid);
      $('#frmSlots').show();
      $('.loading').hide();
    },
    error: function (data) {
      console.log(data);
    },
  });
}
//
$(document).on('click', '.AddSlot', function (event) {
  $('#txtSlotTitle').val('');
  $('#txtSlotUniMorphTags').val('');
  $('#txtSlotFormula').val('');
  $('#SlotId').val('');
  $('#cmbSlotAgreementGroup').val('0');
  $('#btnSlotsSubmit').html('Add');
  $('#SlotPClassId').val($(this).attr('data-PC'));
  $('#frmSlots').show();
  updateSlotUMselects([]);
});

$('#btnSlotsCancel').click(function () {
  $('#frmSlots').hide();
});

function SlotsSubmit() {
  if ($('#btnSlotsSubmit').html() == 'Add') {
    insertSlot();
  } else {
    updateSlot();
  }
}

function insertSlot() {
  $('.loading').show();
  let PC = $('#SlotPClassId').val();
  let title = $('#txtSlotTitle').val().trim();
  let priority = $('#cmbSlotPriority').val();
  $.ajax({
    url: '/Slot/insert',
    type: 'POST',
    data: {
      title: title,
      unimorphtags: UM_Sort($('#txtSlotUniMorphTags').val().trim()),
      formula: $('#txtSlotFormula').val().trim(),
      agreementgroupid: $('#cmbSlotAgreementGroup').val(),
      paradigmclassid: PC,
      priority: priority,
      paradigmclassid: PC,
    },
    success: function (data) {
      $('.loading').hide();
      $('#frmSlots').hide();
      let sublist = $('#PC_' + PC).children('ul');
      let newItem = $(`<li class="" id="S_${data}">
        <span class="priority${priority}"></span>
        <span>${title}</span>
        </li>`);
      sublist.append(newItem);
    },
    error: function (data) {
      alert('Error');
    },
  });
}

function updateSlot() {
  $('.loading').show();
  let PC = $('#SlotPClassId').val();
  let title = $('#txtSlotTitle').val().trim();
  let priority = $('#cmbSlotPriority').val();
  $.ajax({
    url: '/Slot/update',
    type: 'POST',
    data: {
      id: $('#SlotId').val(),
      title: title,
      unimorphtags: UM_Sort($('#txtSlotUniMorphTags').val().trim()),
      formula: $('#txtSlotFormula').val().trim(),
      agreementgroupid: $('#cmbSlotAgreementGroup').val(),
      paradigmclassid: PC,
      priority: priority,
      paradigmclassid: PC,
    },
    success: function (data) {
      $('.loading').hide();
      $('#frmSlots').hide();
      let e = $('#PC_' + PC);
      e.children('ul').children('li').remove();
      e.attr('data-opened', 'false');
      GetSlots(e);
    },
    error: function (data) {
      alert('Error');
    },
  });
}

function ParadigmClassSubmit() {
  if ($('#btnParadigmClassSubmit').html() == 'Add') {
    insertParadigmClass();
  } else {
    updateParadigmClass();
  }
}

$('#btnParadigmClassCancel').click(function () {
  $('#frmParadigmClass').hide();
});

$('#btnAddParadigmClass').click(function () {
  $('#frmParadigmClass').show();
  $('#txtParadigmClassTitle').val('');
  $('#txtParadigmClassDescription').val('');
  $('#ParadigmClassId').val('');
  $('#btnParadigmClassSubmit').html('Add');
});

function insertParadigmClass() {
  $('.loading').show();
  let title = $('#txtParadigmClassTitle').val().trim();
  let description = $('#txtParadigmClassDescription').val().trim();
  $.ajax({
    url: '/ParadigmClass/insert',
    type: 'POST',
    data: {
      title: title,
      langid: myLang.id,
      description: description,
    },
    success: function (data) {
      ParadigmClasses.push({ Id: data, Title: title });
      $('#frmParadigmClass').hide();
      $('#lstParadigmClasses').append(`<li id="PC_${data}">
        <div class="list-title">
          <div id="PCT_${data}" class="paradigmClass">${title}</div>
          <div>
            <span class="smallButton AddSlot" data-PC="${data}"><span class="material-icons">add_circle</span></span>
            <span class="smallButton" onclick="EditParadigmClass(${data})"><span class="material-icons">edit</span></span>        
          </div>
        </div>
        <ul class="slots"></ul>
        </li>`);
      $('.loading').hide();
    },
    error: function (data) {
      alert('Error');
    },
  });
}

function EditParadigmClass(id) {
  $('#frmParadigmClass').show();
  $('#btnParadigmClassSubmit').html('Save');
  $('#txtParadigmClassTitle').val(
    ParadigmClasses.find((x) => x.id == id).title
  );
  $('#txtParadigmClassDescription').val(
    ParadigmClasses.find((x) => x.id == id).description
  );
  $('#ParadigmClassId').val(id);
}

function updateParadigmClass() {
  let id = $('#ParadigmClassId').val();
  let title = $('#txtParadigmClassTitle').val().trim();
  $('.loading').show();
  $.ajax({
    url: '/ParadigmClass/update',
    type: 'POST',
    data: {
      id: id,
      title: title,
      langid: myLang.id,
      description: $('#txtParadigmClassDescription').val().trim(),
    },
    success: function (data) {
      $('.loading').hide();
      ParadigmClasses.find((x) => x.id == id).title = title;
      $('#PCT_' + id).html(title);
      $('#frmParadigmClass').hide();
    },
    error: function (data) {
      alert('Error');
    },
  });
}

// UMtagSelector
Dimensions.forEach((dim) => {
  let dim_id = 'sDim_' + dim.replace(' ', '_');
  $('#SlotUMtagSelector').append(
    `<select class="UMtagSelector slot" id="${dim_id}"><option value="">--${dim}--</option></select>`
  );
});

UM.forEach((tag) => {
  let dim = tag.d;
  let feat = tag.f;
  $('#sDim_' + dim.replace(' ', '_')).append(
    `<option value="${tag.l}">${feat}</option>`
  );
});

$('.UMtagSelector.slot').change(function () {
  tagset = [];
  $('.UMtagSelector.slot').css('background', '');
  for (let i = 0; i < Dimensions.length; i++) {
    let dim_id = '#sDim_' + Dimensions[i].replace(' ', '_');
    if ($(dim_id).val()) {
      $(dim_id).css('background', '#4f0');
      tagset.push($(dim_id).val());
    }
  }
  $('#txtSlotUniMorphTags').val(tagset.join(';'));
});

function updateSlotUMselects(tagset) {
  $('.UMtagSelector.slot').val('');
  $('.UMtagSelector.slot').css('background', '');
  tagset.forEach((tag) => {
    if (tag) {
      let dimension = UM.find((item) => item.l === tag).d;
      let label = UM.find((item) => item.l === tag).l;
      let dim_id = '#sDim_' + dimension.replace(' ', '_');
      $(dim_id).val(label);
      $(dim_id).css('background', '#4f0');
    }
  });
}
// ========= import Slots
// show import form
$('#btnImportSlot').click(function () {
  $('#frmImportSlots').show();
});
// cancel import form
$('#btnSlotImportCancel').click(function () {
  $('#frmImportSlots').hide();
});
// submit import form
function SlotImportSubmit() {
  $('.loading').show();
  $.ajax({
    url: '/Slot/import',
    type: 'POST',
    data: {
      file: $('#txtSlotImport').val(),
      langid: myLang.id,
    },
    success: function (data) {
      $('.loading').hide();
      $('#frmImportSlots').hide();
      getParadigmClasses(myLang.id);
    },
    error: function (data) {
      alert('Error');
    },
  });
}
// export slots
$('#btnExportSlot').click(async function () {
  let file = '';
  for (const pc of ParadigmClasses) {
    file += '#' + pc.title + '\n';
    try {
      const response = await fetch(
        '/Slot/list?' +
          new URLSearchParams({
            ParadigmClassID: pc.id,
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
      console.error('Error fetching slot list:', error);
    }
  }
  const blob = new Blob([file], { type: 'text/plain' });
  const link = document.createElement('a');
  link.href = URL.createObjectURL(blob);
  link.download = myLang.title + '_Slots.tsv';
  link.click();
});

$(document).ready(function () {
  getParadigmClasses(myLang.id);
});
