<script lang="ts">
  import { auth } from '../../lib/stores/auth';
  import { selectedLang } from '../../lib/stores/language';
  import { api } from '../../lib/api';

  let learningTime: number | null = null;
  let contributingTime: number | null = null;
  let expertLevel = 1;
  let pedagogicalScore = 3;
  let efficiencyScore = 3;
  let satisfactionScore = 4;
  let easeScore = 4;
  let feedback = '';

  let isSubmitted = false;
  let isSubmitting = false;
  let errorMessage = '';

  async function handleSurveySubmit() {
    if (!$selectedLang) {
      errorMessage = 'Please select a language variety on the Dashboard before submitting the survey.';
      return;
    }
    isSubmitting = true;
    errorMessage = '';

    try {
      await api.post('/Survey/submit', {
        role: $auth.role,
        langid: $selectedLang.id,
        learningtime: learningTime,
        contributingtime: contributingTime,
        expertlevel: expertLevel,
        pedagogicalscore: pedagogicalScore,
        efficiencyscore: efficiencyScore,
        satisfactionscore: satisfactionScore,
        easescore: easeScore,
        feedback: feedback.trim(),
      });
      isSubmitted = true;
    } catch (err: any) {
      errorMessage = err.message || 'Failed to submit survey. You may have already submitted for this variety.';
    } finally {
      isSubmitting = false;
    }
  }
</script>

<div class="survey-page container" style="max-width: 720px;">
  <div class="card" style="padding: 2.25rem;">
    <h1>Contributor Experience Survey</h1>
    <p style="color: var(--text-muted);">
      Please complete this survey once you have concluded your contribution session. Your input directly helps improve collaborative linguistic tooling.
    </p>

    {#if isSubmitted}
      <div style="text-align: center; padding: 2rem 0;">
        <span class="material-icons" style="font-size: 3.5rem; color: var(--success);">task_alt</span>
        <h2>Thank You for Your Feedback!</h2>
        <p style="color: var(--text-muted);">
          Your survey responses have been recorded and help support open participatory science.
        </p>
        <a href="/app/dashboard" class="button primary" style="margin-top: 1rem;">Return to Dashboard</a>
      </div>
    {:else}
      {#if errorMessage}
        <div class="alert alert-error">{errorMessage}</div>
      {/if}

      <form on:submit|preventDefault={handleSurveySubmit}>
        <div class="survey-section">
          <strong>Contribution Context:</strong>
          <div style="display: flex; gap: 1.5rem; margin-top: 0.5rem; font-size: 0.95rem;">
            <div>Role: <span class="badge primary" style="text-transform: capitalize;">{$auth.role}</span></div>
            <div>Language: <b>{$selectedLang?.title || 'None selected'}</b></div>
          </div>
        </div>

        <div class="field">
          <label for="survey-learn-time">How many minutes did it take you to learn the CommonMorph interface?</label>
          <input id="survey-learn-time" type="number" min="0" bind:value={learningTime} placeholder="e.g. 10" required />
        </div>

        <div class="field">
          <label for="survey-contrib-time">How many hours in total have you spent contributing to this project?</label>
          <input id="survey-contrib-time" type="number" step="0.1" min="0" bind:value={contributingTime} placeholder="e.g. 2.5" required />
        </div>

        <div class="field">
          <span class="field-title">How would you describe your knowledge of linguistics?</span>
          <div class="radio-stack">
            <label><input type="radio" bind:group={expertLevel} value={1} /> 1 — None at all</label>
            <label><input type="radio" bind:group={expertLevel} value={2} /> 2 — Basic (high school grammar)</label>
            <label><input type="radio" bind:group={expertLevel} value={3} /> 3 — Moderate (linguistics enthusiast)</label>
            <label><input type="radio" bind:group={expertLevel} value={4} /> 4 — Advanced (studied linguistics at university)</label>
            <label><input type="radio" bind:group={expertLevel} value={5} /> 5 — Expert (professional field linguist / academic)</label>
          </div>
        </div>

        <div class="field">
          <span class="field-title">How much did the system help you learn more about your language? (1 = Not at all, 5 = Very much)</span>
          <div class="score-buttons">
            {#each [1, 2, 3, 4, 5] as score}
              <button 
                type="button" 
                class="score-btn" 
                class:active={pedagogicalScore === score}
                on:click={() => pedagogicalScore = score}
              >
                {score}
              </button>
            {/each}
          </div>
        </div>

        <div class="field">
          <span class="field-title">How efficient was the project in collecting and eliciting data? (1 = Not efficient, 5 = Extremely efficient)</span>
          <div class="score-buttons">
            {#each [1, 2, 3, 4, 5] as score}
              <button 
                type="button" 
                class="score-btn" 
                class:active={efficiencyScore === score}
                on:click={() => efficiencyScore = score}
              >
                {score}
              </button>
            {/each}
          </div>
        </div>

        <div class="field">
          <span class="field-title">Overall contributor satisfaction:</span>
          <div class="score-buttons">
            <button type="button" class="score-btn emoji" class:active={satisfactionScore === 1} on:click={() => satisfactionScore = 1}>😠 1</button>
            <button type="button" class="score-btn emoji" class:active={satisfactionScore === 2} on:click={() => satisfactionScore = 2}>🙁 2</button>
            <button type="button" class="score-btn emoji" class:active={satisfactionScore === 3} on:click={() => satisfactionScore = 3}>😐 3</button>
            <button type="button" class="score-btn emoji" class:active={satisfactionScore === 4} on:click={() => satisfactionScore = 4}>🙂 4</button>
            <button type="button" class="score-btn emoji" class:active={satisfactionScore === 5} on:click={() => satisfactionScore = 5}>😄 5</button>
          </div>
        </div>

        <div class="field">
          <span class="field-title">How easy was it to contribute?</span>
          <div class="score-buttons">
            {#each [1, 2, 3, 4, 5] as score}
              <button 
                type="button" 
                class="score-btn" 
                class:active={easeScore === score}
                on:click={() => easeScore = score}
              >
                {score}
              </button>
            {/each}
          </div>
        </div>

        <div class="field">
          <label for="survey-feedback">Additional suggestions, feedback, or comments:</label>
          <textarea id="survey-feedback" bind:value={feedback} rows="4" placeholder="Any suggestions to make the platform better..."></textarea>
        </div>

        <button type="submit" class="button primary" style="width: 100%; margin-top: 1rem;" disabled={isSubmitting}>
          {#if isSubmitting}Submitting...{:else}Submit Survey Response{/if}
        </button>
      </form>
    {/if}
  </div>
</div>

<style>
  .survey-section {
    background: #f8fafc;
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 1rem;
    margin-bottom: 1.5rem;
  }

  .field-title {
    display: block;
    font-size: 0.875rem;
    font-weight: 500;
    margin-bottom: 0.35rem;
    color: var(--text);
  }

  .radio-stack {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    margin-top: 0.4rem;
  }

  .radio-stack label {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-weight: 400;
    cursor: pointer;
    margin: 0;
  }

  .score-buttons {
    display: flex;
    gap: 0.5rem;
    margin-top: 0.4rem;
  }

  .score-btn {
    flex: 1;
    padding: 0.6rem;
    background: #fff;
    border: 1px solid var(--border);
    border-radius: 6px;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.15s ease;
  }

  .score-btn.emoji {
    font-size: 0.95rem;
  }

  .score-btn:hover {
    border-color: var(--primary);
  }

  .score-btn.active {
    background: var(--primary);
    color: #fff;
    border-color: var(--primary);
  }
</style>
