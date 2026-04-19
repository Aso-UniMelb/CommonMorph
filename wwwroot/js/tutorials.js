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
  <dt>Inflection Class</dt>
  <dd>A category within a part-of-speech.</dd>
  <dd class="example">1st, 2nd, 3rd, 4th conjugations in Latin are four Inflection Classes.</dd>

  <dt>Lemma</dt>
  <dd>The headword or dictionary form of a word. Each lemma belongs to one Inflection Class.</dd>
  <dd class="example">"amō"</dd>

  <dt>Stem</dt>
  <dd>A base form derived from a lemma. Each lemma can have zero or more Stems. Each Stem can be used in formation of
    different Structures.</dd>
  <dd class="example">"am", "amāv", "amāt"</dd>

  <dt>Structure</dt>
  <dd>A Structure is a grammatical category (e.g., TAM features for verbs) that is expressed by combining a Lemma or Stem
    with a Reusable Layer and other morphemes according to a specific formula. Each Inflection class has one or more
    Structures.</dd>
  <dd class="example">Active Indicative Present, Active Subjunctive Pluperfect</dd>

  <dt>Reusable Layer</dt>
  <dd>A label for a set of affixes (endings) that are used together in one or more Structures. Reusable Layers help to
    associate Structures with the appropriate Affixes. One Reusable Layer can appear in multiple Structures.</dd>
  <dd class="example">Future endings</dd>

  <dt>Affixes</dt>
  <dd>An individual affix (morpheme) within a Reusable Layer that marks grammatical features such as person, number,
    and gender in a Structure.</dd>
  <dd class="example">"ābō" (1st person Singular in "Future endings" Layer)</dd>
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
  <img src="/img/lexicon.png">
  <figcaption></figcaption>
</figure>
<p>
  Lemmas should be added after the inflection classes have been defined, since each lemma must be assigned to a specific
  inflection class.
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
  InflectionClasses: {
    title: 'Linguist Panel: Inflection Classes (for Linguists)',
    content: `
<figure>
  <img src="/img/inflection_classes.png">
  <figcaption></figcaption>
</figure>
<strong>Add/Edit Structure:</strong>
<figure>
  <img src="/img/edit_structure.png">
  <figcaption></figcaption>
</figure>
<p>
  The <b>title</b> is a conventional label that may be recognized by speakers familiar with the language's grammatical
  system, and it can assist them in understanding the structure more easily.
</p>
<p>
  The <b>formula</b> is optional and can be used to generate more accurate suggestions during the early stages of
  elicitation. In the formula, capital letters are placeholders that will be replaced by an affix morpheme, a
  stem, or the lemma. If "A" exists in the formula, the appropriate affix is selected from the members of the
  corresponding Reusable Layer. If no formula is provided, the suggestion will simply copy the lemma, which is
  generally not very informative or useful.
</p>`,
  },
  // ===================
  ReusableLayers: {
    title: 'Linguist Panel: Reusable Layers (for Linguists)',
    content: `
<figure>
  <img src="/img/reusable_layers.png">
  <figcaption></figcaption>
</figure>
<h4>Add/Edit Affixes:</h4>
<figure>
  <img src="/img/edit_affix.png">
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
  tutorials.InflectionClasses,
  tutorials.ReusableLayers,
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
