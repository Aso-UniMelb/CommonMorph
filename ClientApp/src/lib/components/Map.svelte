<script lang="ts">
  import { onMount, onDestroy } from 'svelte';
  import L from 'leaflet';

  export let height = '450px';
  export let mapData: Array<{ id: number; title: string; code: string; latitude?: number; longitude?: number }> = [];
  export let onSelectLanguage: (id: number) => void = () => {};

  let mapContainer: HTMLElement;
  let map: L.Map | null = null;
  let markersLayer: L.LayerGroup | null = null;

  onMount(() => {
    map = L.map(mapContainer).setView([25, 10], 2);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 18,
      attribution: '© OpenStreetMap contributors',
    }).addTo(map);

    markersLayer = L.layerGroup().addTo(map);
    renderMarkers();
  });

  $: if (map && markersLayer && mapData) {
    renderMarkers();
  }

  function renderMarkers() {
    if (!map || !markersLayer) return;
    markersLayer.clearLayers();

    mapData.forEach(item => {
      if (item.latitude !== undefined && item.longitude !== undefined && item.latitude !== null && item.longitude !== null) {
        const marker = L.marker([item.latitude, item.longitude]);
        const popupContent = document.createElement('div');
        popupContent.innerHTML = `
          <div style="font-family: inherit; padding: 2px;">
            <strong style="font-size: 1rem; color: #175676;">${item.title}</strong>
            <div style="color: #64748b; font-size: 0.85rem; margin-bottom: 6px;">ISO 639-3: ${item.code}</div>
            <a href="/dataset/${item.id}" style="display: inline-block; background: #175676; color: #fff; padding: 4px 8px; border-radius: 4px; font-size: 0.8rem; text-decoration: none;">View Dataset</a>
          </div>
        `;
        marker.bindPopup(popupContent);
        marker.on('click', () => {
          onSelectLanguage(item.id);
        });
        marker.addTo(markersLayer!);
      }
    });
  }

  onDestroy(() => {
    if (map) {
      map.remove();
      map = null;
    }
  });
</script>

<div class="map-wrapper" style="height: {height};" bind:this={mapContainer}></div>

<style>
  .map-wrapper {
    width: 100%;
    border-radius: var(--radius);
    border: 1px solid var(--border);
    overflow: hidden;
    z-index: 10;
  }
</style>
