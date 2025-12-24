window.pp = {
  getName: function () {
    // Prefer server cookie; fall back to client cookie
    const server = document.cookie.match(/(?:^|; )planningpoker_username=([^;]+)/);
    if (server) return decodeURIComponent(server[1]);
    const client = document.cookie.match(/(?:^|; )pp_name=([^;]+)/);
    return client ? decodeURIComponent(client[1]) : null;
  },
  setName: function (name) {
    // Client-side persistence (legacy)
    document.cookie = "pp_name=" + encodeURIComponent(name) + "; path=/; max-age=" + (60*60*24*365);
  },
  // Set the cookie the server reads
  setServerName: function (name) {
    document.cookie = "planningpoker_username=" + encodeURIComponent(name) + "; path=/; max-age=" + (60*60*24*30) + "; SameSite=Lax";
  }
};