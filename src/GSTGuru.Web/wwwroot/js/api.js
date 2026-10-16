const base = "";

async function request(path, options = {}) {
  const res = await fetch(base + path, {
    headers: {
      "Content-Type": "application/json",
      ...(options.headers || {}),
    },
    ...options,
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || res.statusText);
  }
  const contentType = res.headers.get("content-type") || "";
  if (contentType.includes("application/json")) {
    return res.json();
  }
  if (contentType.includes("text/csv")) {
    return res.blob();
  }
  return res.text();
}

export const api = {
  dashboard: (from, to) => request(`/api/reports/dashboard${from || to ? "?" + new URLSearchParams({ ...(from ? { from } : {}), ...(to ? { to } : {}) }).toString() : ""}`),
  summary: (from, to) => request(`/api/reports/summary${qs(from,to)}`),
  gstr1: (from, to) => request(`/api/reports/gstr1${qs(from,to)}`),
  hsnSummary: (from, to) => request(`/api/reports/hsn${qs(from,to)}`),
  gstr3b: (from, to) => request(`/api/reports/gstr3b${qs(from,to)}`),
  exportGstr1: async (from, to) => {
    const blob = await request(`/api/reports/gstr1/export.csv${qs(from,to)}`);
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `GSTR-1.csv`;
    a.click();
    URL.revokeObjectURL(url);
  },
  products: {
    list: (search, activeOnly) => request(`/api/products?${new URLSearchParams({ ...(search ? { search } : {}), activeOnly: activeOnly ? "true" : "false" }).toString()}`),
    create: (data) => request("/api/products", { method: "POST", body: JSON.stringify(data) }),
    update: (id, data) => request(`/api/products/${id}`, { method: "PUT", body: JSON.stringify(data) }),
    remove: (id) => request(`/api/products/${id}`, { method: "DELETE" }),
  },
  parties: {
    list: (search, type) => request(`/api/parties?${new URLSearchParams({ ...(search ? { search } : {}), ...(type ? { partyType: type } : {}), activeOnly: "true" }).toString()}`),
    all: () => request(`/api/parties?activeOnly=true`),
    create: (data) => request("/api/parties", { method: "POST", body: JSON.stringify(data) }),
    update: (id, data) => request(`/api/parties/${id}`, { method: "PUT", body: JSON.stringify(data) }),
  },
  invoices: {
    list: (from, to, status) => request(`/api/invoices${qs(from,to,status)}`),
    get: (id) => request(`/api/invoices/${id}`),
    create: (data) => request("/api/invoices", { method: "POST", body: JSON.stringify(data) }),
    issue: (id) => request(`/api/invoices/${id}/issue`, { method: "POST" }),
    cancel: (id) => request(`/api/invoices/${id}/cancel`, { method: "POST" }),
    remove: (id) => request(`/api/invoices/${id}`, { method: "DELETE" }),
  },
  tax: {
    calculate: (data) => request("/api/tax/calculate", { method: "POST", body: JSON.stringify(data) }),
    hsn: (search) => request(`/api/tax/hsn?search=${encodeURIComponent(search||"")}`),
    guessHsn: (desc) => request(`/api/tax/guess-hsn?description=${encodeURIComponent(desc||"")}`),
    statecodes: () => request("/api/tax/statecodes"),
  },
  business: {
    get: () => request("/api/business"),
    save: (data) => request("/api/business", { method: "PUT", body: JSON.stringify(data) }),
  },
  chat: {
    status: () => request("/api/chat/status"),
    send: (messages) => request("/api/chat", { method: "POST", body: JSON.stringify({ messages }) }),
    suggestions: () => request("/api/chat/suggestions"),
    taxRate: (description) => request("/api/chat/tax-rate", { method: "POST", body: JSON.stringify({ description }) }),
  },
};

function qs(from,to,status){
  const p = new URLSearchParams();
  if(from) p.set("from", from);
  if(to) p.set("to", to);
  if(status) p.set("status", status);
  return p.size ? "?" + p.toString() : "";
}
