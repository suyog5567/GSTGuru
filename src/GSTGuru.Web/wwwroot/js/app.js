import { api } from "./api.js";

/* ---------------------------------------------------------------- helpers */
const el = (id) => document.getElementById(id);
const money = (n) => "₹" + Number(n || 0).toLocaleString("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const num = (v) => {
  const n = Number(v);
  return Number.isFinite(n) ? n : 0;
};
const today = () => new Date().toISOString().substring(0, 10);
const addMonths = (d, m) => {
  const x = new Date(d);
  x.setMonth(x.getMonth() + m);
  return x.toISOString().substring(0, 10);
};
const esc = (s) => {
  const d = document.createElement("div");
  d.textContent = s == null ? "" : String(s);
  return d.innerHTML;
};
const debounce = (fn, ms = 250) => {
  let t;
  return (...a) => {
    clearTimeout(t);
    t = setTimeout(() => fn(...a), ms);
  };
};

/* ---------------------------------------------------------------- routing */
const views = document.querySelectorAll(".view");
const navLinks = document.querySelectorAll(".nav-link");

const VIEW_META = {
  dashboard: ["Dashboard", "Overview of invoices, tax collected and top parties."],
  invoices: ["Invoices", "All invoices with filters. Drafts can be issued, issued ones cancelled."],
  "invoice-form": ["Create Invoice", "Auto-computes CGST/SGST (intra-state) or IGST (inter-state)."],
  products: ["Products", "Catalogue with HSN codes and default GST rates."],
  parties: ["Parties", "Customers and suppliers, registered (B2B) or unregistered (B2C)."],
  calculator: ["Quick GST Calculator", "Standalone intra-state vs inter-state breakdown."],
  reports: ["Reports & Returns", "Tax summary, rate-wise, HSN summary, GSTR-1 and GSTR-3B."],
  assistant: ["AI Assistant", "Ask GST questions or request an indicative HSN/rate."],
  business: ["Business Profile", "Seller details used on invoices and reports."],
};

const loaders = {
  dashboard: loadDashboard,
  invoices: loadInvoices,
  products: loadProducts,
  parties: loadParties,
  reports: loadReports,
  business: loadBusiness,
  calculator: () => {},
  assistant: () => {},
};

function showView(name) {
  const id = "view-" + name;
  views.forEach((v) => v.classList.toggle("active", v.id === id));
  navLinks.forEach((l) => l.classList.toggle("active", l.dataset.view === name));
  const meta = VIEW_META[name] || [name, ""];
  el("page-title").textContent = meta[0];
  el("page-subtitle").textContent = meta[1];
  const load = loaders[name];
  if (load) load();
}

navLinks.forEach((l) => l.addEventListener("click", () => showView(l.dataset.view)));

/* ---------------------------------------------------------------- dashboard */
async function loadDashboard() {
  try {
    const d = await api.dashboard();
    el("dashboard-cards").innerHTML = [
      ["Total Invoices", d.totalInvoices, false],
      ["Total Taxable Value", d.totalTaxableValue, true],
      ["Total Tax Collected", d.totalTaxCollected, true],
      ["Total Grand Value", d.totalGrandValue, true],
    ]
      .map(([label, value, isMoney]) =>
        `<div class="card"><div class="card-label">${label}</div><div class="card-value">${isMoney ? money(value) : value}</div></div>`)
      .join("");

    const trend = el("trend-bars");
    const max = Math.max(1, ...d.monthlyTaxTrend.map((m) => m.tax));
    trend.innerHTML = d.monthlyTaxTrend
      .map((m) => {
        const h = Math.max(4, Math.round((m.tax / max) * 150));
        return `<div class="trend-bar"><div class="trend-fill" style="height:${h}px"></div>
          <div class="trend-label">${esc(m.month)}</div><div class="trend-value">${money(m.tax)}</div></div>`;
      })
      .join("");

    el("top-parties-table").innerHTML = d.topParties
      .map((t) => `<tr><td>${esc(t.partyName)}</td><td>${t.invoiceCount}</td><td class="right">${money(t.amount)}</td></tr>`)
      .join("") || `<tr><td colspan="3" class="muted">No data yet.</td></tr>`;

    el("recent-invoices-table").innerHTML = d.recentInvoices
      .map((r) => `<tr><td>${esc(r.invoiceNumber)}</td><td>${r.invoiceDate.substring(0, 10)}</td>
        <td>${esc(r.partyName)}</td><td>${r.isRegistered ? "B2B" : "B2C"}</td>
        <td>${esc(r.status)}</td><td class="right">${money(r.grandTotal)}</td></tr>`)
      .join("") || `<tr><td colspan="6" class="muted">No invoices yet.</td></tr>`;
  } catch (e) {
    console.error(e);
  }
}

/* ---------------------------------------------------------------- invoices list */
async function loadInvoices() {
  const from = el("inv-from").value || undefined;
  const to = el("inv-to").value || undefined;
  const status = el("inv-status").value || undefined;
  const invoices = await api.invoices.list(from, to, status);

  el("invoices-table").innerHTML = invoices
    .map((i) => {
      const tax = num(i.cgst) + num(i.sgst) + num(i.igst) + num(i.cess);
      const actions = [`<button class="btn ghost" data-act="view" data-id="${i.id}">View</button>`];
      if (i.status === "Draft") {
        actions.push(`<button class="btn" data-act="issue" data-id="${i.id}">Issue</button>`);
        actions.push(`<button class="btn ghost" data-act="delete" data-id="${i.id}">Delete</button>`);
      }
      if (i.status === "Issued") {
        actions.push(`<button class="btn ghost" data-act="cancel" data-id="${i.id}">Cancel</button>`);
      }
      return `<tr>
        <td>${esc(i.invoiceNumber)}</td>
        <td>${i.invoiceDate.substring(0, 10)}</td>
        <td>${esc(i.partyName)}</td>
        <td>${esc(i.partyGstin || "—")}</td>
        <td>${i.supplyType}</td>
        <td class="right">${money(i.taxableValue)}</td>
        <td class="right">${money(tax)}</td>
        <td class="right">${money(i.roundOff)}</td>
        <td class="right">${money(i.grandTotal)}</td>
        <td>${esc(i.status)}</td>
        <td>${actions.join(" ")}</td>
      </tr>`;
    })
    .join("") || `<tr><td colspan="11" class="muted">No invoices found.</td></tr>`;
}

el("invoices-table").addEventListener("click", async (e) => {
  const btn = e.target.closest("button[data-act]");
  if (!btn) return;
  const id = Number(btn.dataset.id);
  const act = btn.dataset.act;
  try {
    if (act === "view") {
      const inv = await api.invoices.get(id);
      showInvoiceModal(inv);
    } else if (act === "issue") {
      await api.invoices.issue(id);
      await loadInvoices();
    } else if (act === "cancel") {
      if (confirm("Cancel this issued invoice?")) {
        await api.invoices.cancel(id);
        await loadInvoices();
      }
    } else if (act === "delete") {
      if (confirm("Delete this draft invoice?")) {
        await api.invoices.remove(id);
        await loadInvoices();
      }
    }
  } catch (err) {
    alert("Action failed: " + err.message);
  }
});

function showInvoiceModal(inv) {
  const lines = inv.lines
    .map((l) => `<tr>
      <td>${esc(l.description)}</td><td>${esc(l.hsnCode || "—")}</td><td>${esc(l.unit)}</td>
      <td class="right">${l.quantity}</td><td class="right">${money(l.unitPrice)}</td>
      <td class="right">${l.gstRate}%</td><td class="right">${money(l.taxableValue)}</td>
      <td class="right">${money(l.cgst)}</td><td class="right">${money(l.sgst)}</td>
      <td class="right">${money(l.igst)}</td><td class="right">${money(l.lineTotal)}</td></tr>`)
    .join("");
  el("invoice-detail-body").innerHTML = `
    <div class="modal-grid">
      <div class="modal-kv"><span class="label">Invoice No.</span><span>${esc(inv.invoiceNumber)}</span></div>
      <div class="modal-kv"><span class="label">Date</span><span>${inv.invoiceDate.substring(0, 10)}</span></div>
      <div class="modal-kv"><span class="label">Status</span><span>${esc(inv.status)}</span></div>
      <div class="modal-kv"><span class="label">Party</span><span>${esc(inv.partyName)}</span></div>
      <div class="modal-kv"><span class="label">GSTIN</span><span>${esc(inv.partyGstin || "Unregistered (B2C)")}</span></div>
      <div class="modal-kv"><span class="label">Place of Supply</span><span>${esc(inv.placeOfSupplyStateCode)}</span></div>
      <div class="modal-kv"><span class="label">Supply Type</span><span>${esc(inv.supplyType)}</span></div>
      <div class="modal-kv"><span class="label">Category</span><span>${esc(inv.supplyCategory)}</span></div>
      <div class="modal-kv"><span class="label">Reverse Charge</span><span>${inv.reverseCharge ? "Yes" : "No"}</span></div>
    </div>
    <div class="table-wrap">
      <table class="table" style="min-width:800px;">
        <thead><tr><th>Description</th><th>HSN</th><th>Unit</th><th class="right">Qty</th><th class="right">Rate</th><th class="right">GST</th><th class="right">Taxable</th><th class="right">CGST</th><th class="right">SGST</th><th class="right">IGST</th><th class="right">Total</th></tr></thead>
        <tbody>${lines}</tbody>
      </table>
    </div>
    <div class="totals">
      <div>Taxable: <strong>${money(inv.taxableValue)}</strong></div>
      <div>CGST: <strong>${money(inv.cgst)}</strong></div>
      <div>SGST: <strong>${money(inv.sgst)}</strong></div>
      <div>IGST: <strong>${money(inv.igst)}</strong></div>
      <div>Cess: <strong>${money(inv.cess)}</strong></div>
      <div>Round Off: <strong>${money(inv.roundOff)}</strong></div>
      <div>Grand Total: <strong>${money(inv.grandTotal)}</strong></div>
    </div>`;
  el("invoice-detail-modal").classList.add("active");
}

document.querySelectorAll("[data-close='modal']").forEach((b) =>
  b.addEventListener("click", () => el("invoice-detail-modal").classList.remove("active")));

/* ---------------------------------------------------------------- invoice form */
let products = [];
let parties = [];
let stateCodes = [];
let invLines = [];
let invLineKey = 0;

async function bootstrap() {
  try {
    [products, parties, stateCodes] = await Promise.all([api.products.list("", false), api.parties.all(), api.tax.statecodes()]);
  } catch (e) {
    console.error("bootstrap", e);
  }
}

function fillStateSelect(select, selected) {
  select.innerHTML = stateCodes
    .map((s) => `<option value="${s.code}" ${s.code === selected ? "selected" : ""}>${s.code} — ${esc(s.name)}</option>`)
    .join("");
}

function fillPartySelect() {
  const select = el("inv-form-party");
  select.innerHTML =
    `<option value="">— Select party —</option>` +
    parties.map((p) => `<option value="${p.id}">${esc(p.name)}${p.gstin ? " (" + esc(p.gstin) + ")" : " (B2C)"}</option>`).join("");
}

function newInvLine() {
  invLines.push({ key: ++invLineKey, productId: "", description: "", hsnCode: "", unit: "NOS", quantity: 1, unitPrice: 0, discountPercent: 0, gstRate: 18, cessRate: 0 });
  renderInvLines();
}

function renderInvLines() {
  const tbody = el("inv-lines");
  tbody.innerHTML = invLines
    .map((l) => {
      const opts =
        `<option value="">— manual —</option>` +
        products.map((p) => `<option value="${p.id}" ${String(p.id) === String(l.productId) ? "selected" : ""}>${esc(p.name)}</option>`).join("");
      return `<tr data-key="${l.key}">
        <td><select data-field="productId" style="min-width:180px">${opts}</select></td>
        <td><input data-field="description" value="${esc(l.description)}" style="min-width:180px"></td>
        <td><input data-field="hsnCode" value="${esc(l.hsnCode)}" style="width:90px"></td>
        <td><input data-field="unit" value="${esc(l.unit)}" style="width:70px"></td>
        <td><input type="number" min="0" step="0.001" data-field="quantity" value="${l.quantity}" style="width:80px"></td>
        <td><input type="number" min="0" step="0.01" data-field="unitPrice" value="${l.unitPrice}" style="width:110px"></td>
        <td><input type="number" min="0" step="0.01" data-field="discountPercent" value="${l.discountPercent}" style="width:80px"></td>
        <td><input type="number" min="0" step="0.25" data-field="gstRate" value="${l.gstRate}" style="width:80px"></td>
        <td><input type="number" min="0" step="0.25" data-field="cessRate" value="${l.cessRate}" style="width:80px"></td>
        <td class="right" data-cell="taxableValue">—</td>
        <td class="right" data-cell="cgst">—</td>
        <td class="right" data-cell="sgst">—</td>
        <td class="right" data-cell="igst">—</td>
        <td class="right" data-cell="cess">—</td>
        <td class="right" data-cell="lineTotal">—</td>
        <td><button class="btn ghost" data-remove="${l.key}">✕</button></td>
      </tr>`;
    })
    .join("");
  recomputeInvoice();
}

const recomputeInvoice = debounce(async () => {
  const cat = num(el("inv-form-category").value) || 1;
  const body = {
    sellerStateCode: el("inv-form-seller-state").value || "27",
    placeOfSupplyStateCode: el("inv-form-pos-state").value || "27",
    supplyCategory: cat,
    lines: invLines.map((l) => ({
      description: l.description || "Item",
      hsnCode: l.hsnCode,
      unit: l.unit,
      quantity: num(l.quantity),
      unitPrice: num(l.unitPrice),
      discountPercent: num(l.discountPercent),
      gstRate: num(l.gstRate),
      cessRate: num(l.cessRate),
    })),
  };
  if (!body.lines.length) {
    setInvoiceTotals({});
    return;
  }
  try {
    const result = await api.tax.calculate(body);
    result.lines.forEach((r, i) => {
      const row = el("inv-lines").querySelector(`tr[data-key="${invLines[i].key}"]`);
      if (!row) return;
      ["taxableValue", "cgst", "sgst", "igst", "cess", "lineTotal"].forEach((f) => {
        const cell = row.querySelector(`[data-cell="${f}"]`);
        if (cell) cell.textContent = money(r[f]);
      });
    });
    setInvoiceTotals(result);
  } catch (e) {
    console.error("calc", e);
  }
}, 250);

function setInvoiceTotals(t) {
  el("tot-taxable").textContent = money(t.taxableValue);
  el("tot-cgst").textContent = money(t.cgst);
  el("tot-sgst").textContent = money(t.sgst);
  el("tot-igst").textContent = money(t.igst);
  el("tot-cess").textContent = money(t.cess);
  el("tot-round").textContent = money(t.roundOff);
  el("tot-grand").textContent = money(t.grandTotal);
}

el("inv-lines").addEventListener("input", (e) => {
  const row = e.target.closest("tr[data-key]");
  if (!row) return;
  const key = Number(row.dataset.key);
  const line = invLines.find((l) => l.key === key);
  if (!line) return;
  const field = e.target.dataset.field;
  if (!field) return;
  line[field] = e.target.value;
  recomputeInvoice();
});

el("inv-lines").addEventListener("change", (e) => {
  const row = e.target.closest("tr[data-key]");
  if (!row) return;
  const key = Number(row.dataset.key);
  const line = invLines.find((l) => l.key === key);
  if (!line) return;
  if (e.target.dataset.field === "productId") {
    line.productId = e.target.value;
    const p = products.find((x) => String(x.id) === String(e.target.value));
    if (p) {
      line.description = p.name;
      line.hsnCode = p.hsnCode || "";
      line.unit = p.unit || "NOS";
      line.unitPrice = p.unitPrice;
      line.gstRate = p.defaultGstRate;
      line.cessRate = p.defaultCessRate;
      renderInvLines();
    } else {
      recomputeInvoice();
    }
  }
});

el("inv-lines").addEventListener("click", (e) => {
  const btn = e.target.closest("button[data-remove]");
  if (!btn) return;
  const key = Number(btn.dataset.remove);
  invLines = invLines.filter((l) => l.key !== key);
  renderInvLines();
});

el("inv-add-line").addEventListener("click", newInvLine);
el("inv-cancel").addEventListener("click", () => showView("invoices"));

el("inv-form-party").addEventListener("change", (e) => {
  const p = parties.find((x) => String(x.id) === String(e.target.value));
  if (p) el("inv-form-pos-state").value = p.stateCode;
  recomputeInvoice();
});

["inv-form-seller-state", "inv-form-pos-state", "inv-form-category"].forEach((id) =>
  el(id).addEventListener("input", recomputeInvoice));

async function resetInvoiceForm() {
  await bootstrap();
  invLines = [];
  invLineKey = 0;
  el("inv-form-date").value = today();
  el("inv-form-notes").value = "";
  el("inv-form-category").value = "1";
  el("inv-form-reverse").value = "false";
  fillPartySelect();
  try {
    const biz = await api.business.get();
    el("inv-form-seller-state").value = biz.stateCode || "27";
  } catch {
    el("inv-form-seller-state").value = "27";
  }
  el("inv-form-pos-state").value = el("inv-form-seller-state").value;
  newInvLine();
}

function buildInvoicePayload() {
  return {
    invoiceDate: el("inv-form-date").value || today(),
    partyId: num(el("inv-form-party").value),
    placeOfSupplyStateCode: el("inv-form-pos-state").value,
    sellerStateCode: el("inv-form-seller-state").value,
    supplyCategory: num(el("inv-form-category").value) || 1,
    reverseCharge: el("inv-form-reverse").value === "true",
    notes: el("inv-form-notes").value || null,
    lines: invLines.map((l) => ({
      productId: l.productId ? num(l.productId) : null,
      description: l.description || "Item",
      hsnCode: l.hsnCode || null,
      unit: l.unit || "NOS",
      quantity: num(l.quantity),
      unitPrice: num(l.unitPrice),
      discountPercent: num(l.discountPercent),
      gstRate: num(l.gstRate),
      cessRate: num(l.cessRate),
    })),
  };
}

async function saveInvoice(issueAfter) {
  const payload = buildInvoicePayload();
  if (!payload.partyId) return alert("Please select a party.");
  if (!payload.lines.length) return alert("Please add at least one line item.");
  try {
    const created = await api.invoices.create(payload);
    if (issueAfter) await api.invoices.issue(created.id);
    showView("invoices");
  } catch (e) {
    alert("Could not save invoice: " + e.message);
  }
}

el("inv-save-draft").addEventListener("click", () => saveInvoice(false));
el("inv-issue").addEventListener("click", () => saveInvoice(true));
el("btn-new-invoice").addEventListener("click", () => {
  showView("invoice-form");
  resetInvoiceForm();
});
el("inv-filter").addEventListener("click", loadInvoices);

/* ---------------------------------------------------------------- products */
let editingProductId = null;

async function loadProducts() {
  products = await api.products.list("", false);
  el("products-table").innerHTML =
    products
      .map(
        (p) => `<tr>
        <td>${esc(p.name)}</td><td>${esc(p.hsnCode || "—")}</td><td>${esc(p.unit)}</td>
        <td class="right">${money(p.unitPrice)}</td><td class="right">${p.defaultGstRate}%</td>
        <td class="right">${p.defaultCessRate}%</td><td>${p.isActive ? "Active" : "Inactive"}</td>
        <td><button class="btn ghost" data-edit="${p.id}">Edit</button>
        <button class="btn ghost" data-del="${p.id}">Delete</button></td></tr>`)
      .join("") || `<tr><td colspan="8" class="muted">No products.</td></tr>`;
}

el("products-table").addEventListener("click", async (e) => {
  const editBtn = e.target.closest("button[data-edit]");
  const delBtn = e.target.closest("button[data-del]");
  if (editBtn) {
    const p = products.find((x) => String(x.id) === editBtn.dataset.edit);
    editingProductId = p.id;
    el("prod-form-title").textContent = "Edit Product";
    el("prod-name").value = p.name;
    el("prod-hsn").value = p.hsnCode || "";
    el("prod-unit").value = p.unit;
    el("prod-price").value = p.unitPrice;
    el("prod-gst").value = p.defaultGstRate;
    el("prod-cess").value = p.defaultCessRate;
    el("prod-active").value = String(p.isActive);
    el("prod-form-panel").style.display = "block";
  }
  if (delBtn) {
    if (confirm("Delete this product? If it is used on invoices it will be archived instead.")) {
      try {
        await api.products.remove(Number(delBtn.dataset.del));
        await loadProducts();
      } catch (err) {
        alert("Delete failed: " + err.message);
      }
    }
  }
});

el("prod-add").addEventListener("click", () => {
  editingProductId = null;
  el("prod-form-title").textContent = "Add Product";
  ["prod-name", "prod-hsn", "prod-address"].forEach((id) => (el(id) ? (el(id).value = "") : null));
  el("prod-name").value = "";
  el("prod-hsn").value = "";
  el("prod-unit").value = "NOS";
  el("prod-price").value = 0;
  el("prod-gst").value = 18;
  el("prod-cess").value = 0;
  el("prod-active").value = "true";
  el("prod-form-panel").style.display = "block";
});

el("prod-cancel").addEventListener("click", () => (el("prod-form-panel").style.display = "none"));

el("prod-save").addEventListener("click", async () => {
  const payload = {
    name: el("prod-name").value.trim(),
    hsnCode: el("prod-hsn").value.trim() || null,
    unit: el("prod-unit").value.trim() || "NOS",
    unitPrice: num(el("prod-price").value),
    defaultGstRate: num(el("prod-gst").value),
    defaultCessRate: num(el("prod-cess").value),
    isActive: el("prod-active").value === "true",
  };
  if (!payload.name) return alert("Name is required.");
  try {
    if (editingProductId) await api.products.update(editingProductId, payload);
    else await api.products.create(payload);
    el("prod-form-panel").style.display = "none";
    await loadProducts();
  } catch (e) {
    alert("Save failed: " + e.message);
  }
});

/* ---------------------------------------------------------------- parties */
let editingPartyId = null;

async function loadParties() {
  parties = await api.parties.all();
  fillStateSelect(el("party-state"));
  el("parties-table").innerHTML =
    parties
      .map(
        (p) => `<tr>
        <td>${esc(p.name)}</td><td>${esc(p.gstin || "—")}</td>
        <td>${p.partyType === 1 || p.partyType === "Customer" ? "Customer" : "Supplier"}</td>
        <td>${esc(p.stateCode)} ${esc(p.stateName || "")}</td>
        <td>${esc(p.email || "—")}</td><td>${esc(p.phone || "—")}</td><td>${p.isActive ? "Yes" : "No"}</td>
        <td><button class="btn ghost" data-edit="${p.id}">Edit</button></td></tr>`)
      .join("") || `<tr><td colspan="8" class="muted">No parties.</td></tr>`;
}

el("parties-table").addEventListener("click", (e) => {
  const btn = e.target.closest("button[data-edit]");
  if (!btn) return;
  const p = parties.find((x) => String(x.id) === btn.dataset.edit);
  editingPartyId = p.id;
  el("party-form-title").textContent = "Edit Party";
  el("party-name").value = p.name;
  el("party-gstin").value = p.gstin || "";
  el("party-type").value = String(typeof p.partyType === "number" ? p.partyType : p.partyType === "Supplier" ? 2 : 1);
  fillStateSelect(el("party-state"), p.stateCode);
  el("party-phone").value = p.phone || "";
  el("party-email").value = p.email || "";
  el("party-active").value = String(p.isActive);
  el("party-address").value = p.address || "";
  el("party-form-panel").style.display = "block";
});

el("party-add").addEventListener("click", () => {
  editingPartyId = null;
  el("party-form-title").textContent = "Add Party";
  el("party-name").value = "";
  el("party-gstin").value = "";
  el("party-type").value = "1";
  fillStateSelect(el("party-state"));
  el("party-phone").value = "";
  el("party-email").value = "";
  el("party-active").value = "true";
  el("party-address").value = "";
  el("party-form-panel").style.display = "block";
});

el("party-cancel").addEventListener("click", () => (el("party-form-panel").style.display = "none"));

el("party-save").addEventListener("click", async () => {
  const stateCode = el("party-state").value;
  const stateName = (stateCodes.find((s) => s.code === stateCode) || {}).name || null;
  const payload = {
    name: el("party-name").value.trim(),
    gstin: el("party-gstin").value.trim() || null,
    partyType: num(el("party-type").value) || 1,
    stateCode,
    stateName,
    phone: el("party-phone").value.trim() || null,
    email: el("party-email").value.trim() || null,
    address: el("party-address").value.trim() || null,
    isActive: el("party-active").value === "true",
  };
  if (!payload.name) return alert("Name is required.");
  try {
    if (editingPartyId) await api.parties.update(editingPartyId, payload);
    else await api.parties.create(payload);
    el("party-form-panel").style.display = "none";
    await loadParties();
  } catch (e) {
    alert("Save failed: " + e.message);
  }
});

/* ---------------------------------------------------------------- calculator */
let calcLines = [];
let calcKey = 0;

function addCalcLine() {
  calcLines.push({ key: ++calcKey, description: "Item", quantity: 1, unitPrice: 0, gstRate: 18, cessRate: 0 });
  renderCalcLines();
}

function renderCalcLines() {
  el("calc-lines").innerHTML = calcLines
    .map(
      (l) => `<tr data-key="${l.key}">
      <td><input data-field="description" value="${esc(l.description)}" style="min-width:200px"></td>
      <td><input type="number" min="0" step="0.001" data-field="quantity" value="${l.quantity}" style="width:90px"></td>
      <td><input type="number" min="0" step="0.01" data-field="unitPrice" value="${l.unitPrice}" style="width:120px"></td>
      <td><input type="number" min="0" step="0.25" data-field="gstRate" value="${l.gstRate}" style="width:90px"></td>
      <td><input type="number" min="0" step="0.25" data-field="cessRate" value="${l.cessRate}" style="width:90px"></td>
      <td class="right" data-cell="taxableValue">—</td>
      <td><button class="btn ghost" data-remove="${l.key}">✕</button></td></tr>`)
    .join("");
  runCalc();
}

const runCalc = debounce(async () => {
  if (!calcLines.length) return;
  try {
    const result = await api.tax.calculate({
      sellerStateCode: el("calc-seller").value || "27",
      placeOfSupplyStateCode: el("calc-pos").value || "27",
      supplyCategory: num(el("calc-category").value) || 1,
      lines: calcLines.map((l) => ({
        description: l.description,
        quantity: num(l.quantity),
        unitPrice: num(l.unitPrice),
        gstRate: num(l.gstRate),
        cessRate: num(l.cessRate),
      })),
    });
    result.lines.forEach((r, i) => {
      const row = el("calc-lines").querySelector(`tr[data-key="${calcLines[i].key}"]`);
      if (row) row.querySelector('[data-cell="taxableValue"]').textContent = money(r.taxableValue);
    });
    el("calc-taxable").textContent = money(result.taxableValue);
    el("calc-cgst").textContent = money(result.cgst);
    el("calc-sgst").textContent = money(result.sgst);
    el("calc-igst").textContent = money(result.igst);
    el("calc-cess").textContent = money(result.cess);
    el("calc-grand").textContent = money(result.grandTotal);
  } catch (e) {
    console.error(e);
  }
}, 250);

el("calc-lines").addEventListener("input", (e) => {
  const row = e.target.closest("tr[data-key]");
  if (!row || !e.target.dataset.field) return;
  const line = calcLines.find((l) => l.key === Number(row.dataset.key));
  if (line) line[e.target.dataset.field] = e.target.value;
  runCalc();
});
el("calc-lines").addEventListener("click", (e) => {
  const btn = e.target.closest("button[data-remove]");
  if (!btn) return;
  calcLines = calcLines.filter((l) => l.key !== Number(btn.dataset.remove));
  renderCalcLines();
});
el("calc-add-line").addEventListener("click", addCalcLine);
["calc-seller", "calc-pos", "calc-category"].forEach((id) => el(id).addEventListener("input", runCalc));

function initCalculator() {
  if (!calcLines.length) {
    calcLines.push({ key: ++calcKey, description: "Consulting services", quantity: 1, unitPrice: 25000, gstRate: 18, cessRate: 0 });
    renderCalcLines();
  }
}

/* ---------------------------------------------------------------- reports */
function initReports() {
  if (!el("rep-from").value) el("rep-from").value = addMonths(today(), -3);
  if (!el("rep-to").value) el("rep-to").value = today();
}

async function loadReports() {
  initReports();
  const from = el("rep-from").value;
  const to = el("rep-to").value;
  try {
    const [summary, gstr1, gstr3b] = await Promise.all([api.summary(from, to), api.gstr1(from, to), api.gstr3b(from, to)]);

    el("rep-taxable").textContent = money(summary.taxableValue);
    el("rep-cgst").textContent = money(summary.cgst);
    el("rep-sgst").textContent = money(summary.sgst);
    el("rep-igst").textContent = money(summary.igst);
    el("rep-cess").textContent = money(summary.cess);
    el("rep-grand").textContent = money(summary.grandTotal);

    el("ratewise-table").innerHTML =
      gstr1.rateWise
        .map(
          (r) => `<tr><td>${r.gstRate}%</td><td class="right">${money(r.taxableValue)}</td>
        <td class="right">${money(r.igst)}</td><td class="right">${money(r.cgst)}</td>
        <td class="right">${money(r.sgst)}</td><td class="right">${money(r.cess)}</td></tr>`)
        .join("") || `<tr><td colspan="6" class="muted">No data.</td></tr>`;

    el("hsn-table").innerHTML =
      gstr1.hsnSummary
        .map(
          (h) => `<tr><td>${esc(h.hsnCode)}</td><td class="right">${h.gstRate}%</td><td>${esc(h.unit)}</td>
        <td class="right">${h.quantity}</td><td class="right">${money(h.taxableValue)}</td>
        <td class="right">${money(h.igst)}</td><td class="right">${money(h.cgst)}</td>
        <td class="right">${money(h.sgst)}</td><td class="right">${money(h.cess)}</td>
        <td class="right">${money(h.lineTotal)}</td></tr>`)
        .join("") || `<tr><td colspan="10" class="muted">No data.</td></tr>`;

    el("gstr1-b2b-table").innerHTML =
      gstr1.b2B
        .map(
          (b) => `<tr><td>${esc(b.invoiceNumber)}</td><td>${b.invoiceDate.substring(0, 10)}</td>
        <td>${esc(b.gstin)}</td><td>${esc(b.partyName)}</td><td>${esc(b.placeOfSupplyStateCode)}</td>
        <td class="right">${money(b.taxableValue)}</td><td class="right">${money(b.igst)}</td>
        <td class="right">${money(b.cgst)}</td><td class="right">${money(b.sgst)}</td>
        <td class="right">${money(b.cess)}</td><td class="right">${money(b.grandTotal)}</td></tr>`)
        .join("") || `<tr><td colspan="11" class="muted">No B2B data.</td></tr>`;

    el("g3b-1a").textContent = money(gstr3b.section3_1aTaxable);
    el("g3b-1b").textContent = money(gstr3b.section3_1bZeroRated);
    el("g3b-1c").textContent = money(gstr3b.section3_1cNilExempt);
    el("g3b-tax").textContent = money(
      num(gstr3b.taxPayable.cgst) + num(gstr3b.taxPayable.sgst) + num(gstr3b.taxPayable.igst) + num(gstr3b.taxPayable.cess)
    );
  } catch (e) {
    console.error("reports", e);
  }
}

document.querySelectorAll(".tab").forEach((tab) =>
  tab.addEventListener("click", () => {
    document.querySelectorAll(".tab").forEach((t) => t.classList.toggle("active", t === tab));
    document.querySelectorAll(".tab-content").forEach((c) => c.classList.toggle("active", c.id === "tab-" + tab.dataset.tab));
  }));

el("rep-apply").addEventListener("click", loadReports);
el("rep-export-gstr1").addEventListener("click", () => api.exportGstr1(el("rep-from").value, el("rep-to").value));

/* ---------------------------------------------------------------- business */
async function loadBusiness() {
  try {
    const b = await api.business.get();
    el("biz-legal-name").value = b.legalName || "";
    el("biz-trade-name").value = b.tradeName || "";
    el("biz-gstin").value = b.gstin || "";
    el("biz-state-code").value = b.stateCode || "";
    el("biz-state-name").value = b.stateName || "";
    el("biz-email").value = b.email || "";
    el("biz-phone").value = b.phone || "";
    el("biz-prefix").value = b.invoicePrefix || "INV";
    el("biz-next").value = b.nextInvoiceNumber || 1;
    el("biz-address").value = b.address || "";
  } catch (e) {
    console.error(e);
  }
}

el("biz-save").addEventListener("click", async () => {
  const payload = {
    legalName: el("biz-legal-name").value.trim(),
    tradeName: el("biz-trade-name").value.trim() || null,
    gstin: el("biz-gstin").value.trim() || null,
    stateCode: el("biz-state-code").value.trim(),
    stateName: el("biz-state-name").value.trim() || null,
    email: el("biz-email").value.trim() || null,
    phone: el("biz-phone").value.trim() || null,
    invoicePrefix: el("biz-prefix").value.trim() || "INV",
    nextInvoiceNumber: num(el("biz-next").value) || 1,
    address: el("biz-address").value.trim() || null,
  };
  try {
    await api.business.save(payload);
    alert("Business profile saved.");
  } catch (e) {
    alert("Save failed: " + e.message);
  }
});

/* ---------------------------------------------------------------- AI assistant */
let chatHistory = [];

async function initChat() {
  const messagesEl = el("chat-messages");
  const suggestionsEl = el("chat-suggestions");

  try {
    const status = await api.chat.status();
    el("ai-status-badge").textContent = status.provider + (status.isConfigured ? "" : " (demo)");
    el("chat-provider-info").textContent = "Provider: " + status.provider + (status.isConfigured ? " (configured)" : " (offline demo)");
  } catch {
    el("ai-status-badge").textContent = "Offline (demo)";
  }

  try {
    const sugs = await api.chat.suggestions();
    sugs.forEach((s) => {
      const btn = document.createElement("button");
      btn.className = "chat-chip";
      btn.textContent = s;
      btn.onclick = () => {
        el("chat-input").value = s;
        send();
      };
      suggestionsEl.appendChild(btn);
    });
  } catch {}

  function append(role, content) {
    const msg = document.createElement("div");
    msg.className = "chat-message " + role;
    msg.innerHTML = `<div class="chat-bubble">${esc(content)}</div>
      <div class="chat-meta">${role === "user" ? "You" : "GST Guru"}</div>`;
    messagesEl.appendChild(msg);
    messagesEl.scrollTop = messagesEl.scrollHeight;
  }

  async function send() {
    const text = el("chat-input").value.trim();
    if (!text) return;
    append("user", text);
    chatHistory.push({ role: "user", content: text });
    el("chat-input").value = "";
    try {
      const res = await api.chat.send(chatHistory);
      chatHistory.push({ role: "assistant", content: res.reply });
      let body = res.reply;
      if (res.keyPoints && res.keyPoints.length) body += "\n\nKey points:\n- " + res.keyPoints.join("\n- ");
      if (res.suggestedQuestions && res.suggestedQuestions.length) body += "\n\nYou may also ask:\n- " + res.suggestedQuestions.join("\n- ");
      append("assistant", body);
    } catch (e) {
      append("assistant", "Error: " + e.message);
    }
  }

  el("chat-send").addEventListener("click", send);
  el("chat-input").addEventListener("keydown", (e) => {
    if (e.key === "Enter") send();
  });

  append("assistant", "Hi! I'm GST Guru AI Assistant. Ask me about GST rates, calculations, HSN codes, returns, ITC or e-invoicing.");
}

/* ---------------------------------------------------------------- bootstrap */
el("btn-refresh").addEventListener("click", () => {
  const active = document.querySelector(".nav-link.active");
  showView(active ? active.dataset.view : "dashboard");
});

async function init() {
  await bootstrap();
  initCalculator();
  initReports();
  await loadDashboard();
  await initChat();
}

init();
