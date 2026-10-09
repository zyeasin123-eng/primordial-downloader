export default {
  async fetch(request, env) {
    const url = new URL(request.url);

    const corsHeaders = {
      'Access-Control-Allow-Origin': '*',
      'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
      'Access-Control-Allow-Headers': 'Content-Type',
      'Content-Type': 'application/json; charset=utf-8'
    };

    if (request.method === 'OPTIONS') {
      return new Response(null, { status: 200, headers: corsHeaders });
    }

    const now = Math.floor(Date.now() / 1000);
    const expirySeconds = 120; // 2 minutes auto-prune

    // Endpoint: /heartbeat, /status, or root
    if (url.pathname === '/heartbeat' || url.pathname === '/status' || url.pathname === '/') {
      const id = url.searchParams.get('id');
      const tunnel = url.searchParams.get('tunnel') || '';
      const lanIp = url.searchParams.get('lan_ip') || '';

      // If Cloudflare KV is configured
      if (env && env.NODES_KV) {
        if (id) {
          const cleanId = id.replace(/[^a-zA-Z0-9_-]/g, '').slice(0, 64);
          await env.NODES_KV.put(`node:${cleanId}`, JSON.stringify({
            lastPing: now,
            tunnel,
            lan_ip: lanIp
          }), { expirationTtl: expirySeconds });
        }

        const list = await env.NODES_KV.list({ prefix: 'node:' });
        let activeCount = 0;
        let activeTunnel = '';
        let activeLanIp = '';

        for (const key of list.keys) {
          const val = await env.NODES_KV.get(key.name, 'json');
          if (val && (now - val.lastPing <= expirySeconds)) {
            activeCount++;
            if (!activeTunnel && val.tunnel) activeTunnel = val.tunnel;
            if (!activeLanIp && val.lan_ip) activeLanIp = val.lan_ip;
          }
        }

        return new Response(JSON.stringify({
          status: 'success',
          active_bots: activeCount,
          tunnel: activeTunnel,
          lan_ip: activeLanIp,
          server_time: now,
          edge_provider: 'cloudflare_worker'
        }), { headers: corsHeaders });
      }

      // Standalone edge response (if KV not bound yet)
      return new Response(JSON.stringify({
        status: 'success',
        active_bots: tunnel ? 1 : 0,
        tunnel,
        lan_ip: lanIp,
        server_time: now,
        edge_provider: 'cloudflare_worker'
      }), { headers: corsHeaders });
    }

    return new Response(JSON.stringify({ error: 'Endpoint not found' }), { status: 404, headers: corsHeaders });
  }
};
