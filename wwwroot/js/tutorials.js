let tutorials = {
  welcome: {
    title: 'Welcome to the Common Morph!',
    content: `<p>This tutorial will guide you through the process of contributing to the Common Morph project.</p>
<p>Table of Contents:</p>
<ul id="toc"></ul>
`,
  },
  // ===================
  Terminology: {
    title: 'Terminology (for Linguists)',
    content: `
<p>We explain our terminology using examples from Latin verb conjugation:</p>
<figure>
  <img src="/img/terminology_latin.png" alt="describing terminology on latin conjugation">
  <figcaption>Describing the terminology on Latin conjugation table</figcaption>
</figure>
<dl>
  <dt>Paradigm Class</dt>
  <dd>A category within a part-of-speech.</dd>
  <dd class="example">1st, 2nd, 3rd, 4th conjugations in Latin are four Paradigm Classes.</dd>

  <dt>Lemma</dt>
  <dd>The headword or dictionary form of a word. Each lemma belongs to one Paradigm Class.</dd>
  <dd class="example">"amō"</dd>

  <dt>Stem</dt>
  <dd>A base form derived from a lemma. Each lemma can have zero or more Stems. Each Stem can be used in formation of
    different Slots.</dd>
  <dd class="example">"am", "amāv", "amāt"</dd>

  <dt>Slot</dt>
  <dd>A Slot is a grammatical category (e.g., TAM features for verbs) that is expressed by combining a Lemma or Stem
    with an Agreement Group and other morphemes according to a specific formula. Each Paradigm class has one or more
    Slots.</dd>
  <dd class="example">Active Indicative Present, Active Subjunctive Pluperfect</dd>

  <dt>Agreement Group</dt>
  <dd>A label for a set of affixes (endings) that are used together in one or more Slots. Agreement Groups help to
    associate Slots with the appropriate Agreement Items. One Agreement Group can appear in multiple Slots.</dd>
  <dd class="example">Future endings</dd>

  <dt>Agreement Item</dt>
  <dd>An individual affix (morpheme) within an Agreement Group that marks grammatical features such as person, number,
    and gender in a Slot.</dd>
  <dd class="example">"ābō" (1st person Singular in "Future endings" Group)</dd>
</dl>`,
  },
  // ===================
  Dashboard: {
    title: 'Dashboard',
    content: `
<p>In <a href="/app/dashboard">Dashboard</a> choose your language variety and your preferred elicitation language.</p>
<figure>
  <img src="/img/dashboard.png" alt="common-morph dashborad">
  <figcaption></figcaption>
</figure>`,
  },
  // ===================
  Lemmas: {
    title: 'Linguist Panel: Lemmas (for Linguists)',
    content: `
<figure>
  <img src="/img/lemmas.png">
  <figcaption></figcaption>
</figure>
<p>
  Lemmas should be added after the paradigm classes have been defined, since each lemma must be assigned to a specific
  paradigm class.
</p>
<h4>Add/Edit Lemma:</h4>
<figure>
  <img src="/img/edit_lemma.png">
  <figcaption></figcaption>
</figure>
<p>
  The Meaning in the meta-language is required to create clearer and more understandable elicitation questions.
  Additionally, these meanings will support the development of a global lexicon for future use.
</p>`,
  },
  // ===================
  ParadigmClasses: {
    title: 'Linguist Panel: Paradigm Classes (for Linguists)',
    content: `
<figure>
  <img src="/img/paradigm_classes.png">
  <figcaption></figcaption>
</figure>
<strong>Add/Edit Slot:</strong>
<figure>
  <img src="/img/edit_slot.png">
  <figcaption></figcaption>
</figure>
<p>
  The <b>title</b> is a conventional label that may be recognized by speakers familiar with the language's grammatical
  system, and it can assist them in understanding the structure more easily.
</p>
<p>
  The <b>formula</b> is optional and can be used to generate more accurate suggestions during the early stages of
  elicitation. In the formula, capital letters are placeholders that will be replaced by an agreement item morpheme, a
  stem, or the lemma. If "A" exists in the formula, the appropriate agreement item is selected from the members of the
  corresponding Agreement Group. If no formula is provided, the suggestion will simply copy the lemma, which is
  generally not very informative or useful.
</p>`,
  },
  // ===================
  AgreementGroups: {
    title: 'Linguist Panel: Agreement Groups (for Linguists)',
    content: `
<figure>
  <img src="/img/agreement_groups.png">
  <figcaption></figcaption>
</figure>
<h4>Add/Edit Agreement Items:</h4>
<figure>
  <img src="/img/edit_agreement_item.png">
  <figcaption></figcaption>
</figure>
<p>
  The <b>title</b> is a conventional label that may be recognized by speakers familiar with the language's grammatical
  system, and it can assist them in understanding the structure more easily.
</p>`,
  },
};

let currentStep = 0;

const steps = [
  tutorials.welcome,
  tutorials.Terminology,
  tutorials.Dashboard,
  tutorials.Lemmas,
  tutorials.ParadigmClasses,
  tutorials.AgreementGroups,
];

function nextStep() {
  if (currentStep < steps.length - 1) {
    currentStep++;
    updateContent();
  }
}

function prevStep() {
  if (currentStep > 0) {
    currentStep--;
    updateContent();
  }
}

function updateContent() {
  let content = steps[currentStep].content;
  $('#tutorialText').html(
    `<h2>${steps[currentStep].title}</h2>${steps[currentStep].content}`
  );
  if (currentStep == 0) {
    for (let i = 1; i < steps.length; i++) {
      $('#toc').append(
        `<li onclick="currentStep=${i}; updateContent();">${steps[i].title}</li>`
      );
    }
  }
  $('#backBtn').prop('disabled', currentStep == 0);
  $('#nextBtn').prop('disabled', currentStep == steps.length - 1);
}

// ===================
$(document).ready(function () {
  $('#tutorials').html(`
<div class="tutorial-container">
  <div class="tutorial-content" id="tutorialText"></div>
  <div class="tutorial-buttons">
    <button type="button" class="back-btn" onclick="prevStep()" id="backBtn">Back</button>
    <button type="button" class="next-btn" onclick="currentStep=0; updateContent();" id="tocBtn">Table of Contents</button>
    <button type="button" class="next-btn" onclick="nextStep()" id="nextBtn">Next</button>
  </div>
</div>`);

  updateContent();
});
