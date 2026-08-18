<script lang="ts">
  import { onMount } from 'svelte';
  import { api } from '../lib/api';
  import Map from '../lib/components/Map.svelte';

  let mapData: any[] = [];

  onMount(async () => {
    try {
      mapData = await api.get('/Lang/mapdata');
    } catch (err) {
      console.error('Failed to fetch map data', err);
    }
  });
</script>

<div class="home-page">
  <!-- Hero Section -->
  <section class="hero-section">
    <div class="hero-content">
      <div class="hero-text">
        <h1>Expanding Morphological Resources for Every Language</h1>
        <p class="hero-subtitle">
          CommonMorph is a participatory open platform for collecting, structuring, and validating morphological paradigms—powered collaboratively by field linguists, indigenous & local language communities, and active-learning AI.
        </p>
        <div class="hero-cta-group">
          <a href="/datasets" class="button primary">
            <span class="material-icons">download</span> Explore Datasets
          </a>
          <a href="/user/login" class="button accent">
            <span class="material-icons">volunteer_activism</span> Contribute!
          </a>
        </div>
      </div>
      <div class="hero-graphic">
        <div class="hero-image-frame">
          <img 
            src="/_elicitation.jpg" 
            alt="Field elicitation in action with CommonMorph" 
            class="hero-image" 
            loading="eager"
          />
        </div>
      </div>
    </div>
  </section>

  <!-- How It Works Section -->
  <section class="container" id="how-it-works" style="margin-top: 3rem;">
    <div style="text-align: center; max-width: 650px; margin: 0 auto 2.5rem;">
      <h2>How CommonMorph Works</h2>
      <p style="color: var(--text-muted);">
        A structured three-step human-in-the-loop pipeline connecting linguists and native speakers with iterative machine learning.
      </p>
    </div>

    <div class="tiers-grid">
      <div class="tier-card card">
        <div class="step-badge">1</div>
        <h3>Expert Initialization</h3>
        <p>
          Linguists configure inflectional paradigms, UniMorph features, and reusable agreement layers. Paradigms can be imported and shared across related dialects.
        </p>
      </div>

      <div class="tier-card card">
        <div class="step-badge">2</div>
        <h3>Remote AI Elicitation</h3>
        <p>
          Community speakers provide word forms through intuitive prompts. Neural networks and LLMs generate real-time suggestions that improve with each verified sample.
        </p>
      </div>

      <div class="tier-card card">
        <div class="step-badge">3</div>
        <h3>Community Validation</h3>
        <p>
          Submissions are peer-reviewed and verified by other speakers to resolve dialectal variations, orthographic differences, and ensure cultural integrity.
        </p>
      </div>
    </div>
  </section>

  <!-- Global Coverage Map Section -->
  <section class="container" style="margin-top: 3rem; margin-bottom: 3rem;">
    <div class="card" style="padding: 2rem;">
      <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 1rem; margin-bottom: 1.5rem;">
        <div>
          <h2>Documented Language Varieties</h2>
          <p style="color: var(--text-muted); margin: 0;">
            Interactive map of active language varieties in the CommonMorph registry.
          </p>
        </div>
        <a href="/datasets" class="button secondary">
          <span class="material-icons">list</span> View All Varieties
        </a>
      </div>

      <Map {mapData} height="420px" />
    </div>
  </section>
</div>

<style>
  .hero-section {
    background: linear-gradient(180deg, #edf6fa 0%, #ffffff 100%);
    padding: 4rem 1rem 3rem;
    border-bottom: 1px solid var(--border);
  }

  .hero-content {
    max-width: var(--maxWidth);
    margin: 0 auto;
    display: grid;
    grid-template-columns: 1.2fr 0.8fr;
    gap: 3rem;
    align-items: center;
  }

  .hero-subtitle {
    font-size: 1.1rem;
    color: #475569;
    margin-bottom: 2rem;
    line-height: 1.7;
  }

  .hero-cta-group {
    display: flex;
    gap: 1rem;
    flex-wrap: wrap;
  }

  .tiers-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
    gap: 1.5rem;
  }

  .tier-card {
    position: relative;
    padding-top: 2rem;
  }

  .step-badge {
    position: absolute;
    top: -16px;
    left: 1.5rem;
    width: 34px;
    height: 34px;
    border-radius: 50%;
    background: var(--primary);
    color: #fff;
    font-weight: 700;
    display: flex;
    align-items: center;
    justify-content: center;
    box-shadow: var(--shadow);
  }

  .hero-graphic {
    display: flex;
    justify-content: center;
    align-items: center;
    width: 100%;
  }

  .hero-image-frame {
    position: relative;
    width: 100%;
    max-width: 460px;
    border-radius: 16px;
    overflow: hidden;
    box-shadow: 0 12px 32px rgba(23, 86, 118, 0.16), 0 4px 12px rgba(0, 0, 0, 0.08);
    border: 1px solid rgba(255, 255, 255, 0.8);
    background: #ffffff;
    transition: transform 0.3s ease, box-shadow 0.3s ease;
  }

  .hero-image-frame:hover {
    transform: translateY(-2px);
    box-shadow: 0 16px 40px rgba(23, 86, 118, 0.22), 0 6px 16px rgba(0, 0, 0, 0.1);
  }

  .hero-image {
    display: block;
    width: 100%;
    height: auto;
    aspect-ratio: 1 / 1;
    object-fit: cover;
  }

  @media (max-width: 840px) {
    .hero-content {
      grid-template-columns: 1fr;
      text-align: center;
      gap: 2.25rem;
    }
    .hero-cta-group {
      justify-content: center;
    }
    .hero-image-frame {
      max-width: 400px;
      margin: 0 auto;
    }
  }

  @media (max-width: 480px) {
    .hero-section {
      padding: 2.5rem 1rem 2rem;
    }
    .hero-image-frame {
      max-width: 100%;
      border-radius: 12px;
    }
  }
</style>
