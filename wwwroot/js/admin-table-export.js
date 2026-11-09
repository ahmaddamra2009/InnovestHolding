// «تصدير إلى Excel»: a CSV button for every admin list — each table.admin-table, and each card of inline edit forms
// (form.row-form: «القوائم», «خيارات مجال الاهتمام», «مزايا التسجيل»…). The file holds what the list shows, without
// its actions column; a paged table (its pager carries data-pages / data-base) is exported whole by reading each of
// its pages. A table the server exports itself (data-export="server": leads, contact messages, with every field) or
// that opts out (data-export="none") gets no button. Same CSV rules as the server export (LeadCsv): UTF-8 with a BOM
// so Excel opens Arabic correctly, every cell quoted, and a formula-like cell prefixed with an apostrophe.
(function () {
    'use strict';

    const LABEL = 'تصدير إلى Excel (CSV)';
    const ACTIONS = 'إجراءات';
    const FIELDS = 'input:not([type=hidden]):not([type=file]), select, textarea';

    function formulaLike(v) {
        if (v.length === 0) return false;
        if (v[0] === '\t' || v[0] === '\r') return true;
        const t = v.trimStart();
        return t.length > 0 && '=+-@|＝＋－＠'.includes(t[0]);
    }

    function cell(value) {
        let v = value ?? '';
        if (formulaLike(v)) v = "'" + v;
        return '"' + v.replace(/"/g, '""') + '"';
    }

    const clean = s => (s || '').replace(/\s+/g, ' ').trim();

    function fieldValue(field) {
        if (field.type === 'checkbox' || field.type === 'radio') return field.checked ? 'نعم' : 'لا';
        if (field.tagName === 'SELECT') return clean(field.selectedOptions[0]?.textContent);
        return clean(field.value);
    }

    // A field's name as its label shows it, without the field's own text, hints and the required star (a label not
    // tied to the field: the one in the same .form-group / .form-check).
    function fieldLabel(field) {
        const label = field.labels?.[0] || field.closest('.form-group, .form-check')?.querySelector('label');
        if (!label) return clean(field.getAttribute('aria-label') || field.name);
        const copy = label.cloneNode(true);
        copy.querySelectorAll('input, select, textarea, .req, .field-hint').forEach(n => n.remove());
        return clean(copy.textContent);
    }

    // The text of one table cell: an inline edit form by its fields (one field: its value; several: "label: value |
    // …", empty ones left out), an image by its alt text, else the visible words. Buttons of symbols only (move ▲ ▼)
    // are left out; a worded button («ظاهر») is the cell's value.
    function cellText(td) {
        const fields = [...td.querySelectorAll(FIELDS)];
        if (fields.length === 1) return fieldValue(fields[0]);
        if (fields.length > 1) return fields.filter(f => fieldValue(f) !== '')
            .map(f => `${fieldLabel(f)}: ${fieldValue(f)}`).join(' | ');
        const copy = td.cloneNode(true);
        copy.querySelectorAll('script, style, .sr-only').forEach(n => n.remove());
        copy.querySelectorAll('button').forEach(b => { if (!/[\p{L}\p{N}]/u.test(b.textContent)) b.remove(); });
        const text = clean(copy.textContent);
        if (text) return text;
        return clean(td.querySelector('img')?.getAttribute('alt'));
    }

    // Columns to leave out: the actions column (its header, or cells marked .actions) and any th[data-export=skip].
    function skipped(table) {
        const heads = [...table.querySelectorAll('thead tr:last-child th')];
        const skip = new Set();
        heads.forEach((th, i) => {
            if (clean(th.textContent) === ACTIONS || th.dataset.export === 'skip') skip.add(i);
        });
        table.querySelectorAll('tbody tr').forEach(tr => [...tr.children].forEach((td, i) => {
            if (td.classList.contains('actions')) skip.add(i);
        }));
        return { heads, skip };
    }

    function bodyRows(table, skip) {
        return [...table.querySelectorAll('tbody tr')]
            .filter(tr => !(tr.children.length === 1 && tr.querySelector('[colspan]')))   // not an "empty list" row
            .map(tr => [...tr.children].filter((_, i) => !skip.has(i)).map(cellText));
    }

    // Every page of a paged table: the same table of each page, read from the server (same origin, admin only).
    async function allRows(table, skip) {
        const pager = table.closest('.admin-card')?.querySelector('nav.pager[data-pages]');
        const pages = Number(pager?.dataset.pages || 1);
        if (!pager || pages <= 1) return bodyRows(table, skip);

        const index = [...document.querySelectorAll('table.admin-table')].indexOf(table);
        const rows = [];
        for (let n = 1; n <= pages; n++) {
            const url = new URL(pager.dataset.base, location.origin);
            if (url.origin !== location.origin || !url.pathname.startsWith('/Admin/')) throw new Error('base');
            url.searchParams.set('page', String(n));
            const res = await fetch(url, { credentials: 'same-origin', headers: { 'Accept': 'text/html' } });
            if (!res.ok || new URL(res.url).pathname !== url.pathname) throw new Error('page ' + n);
            const doc = new DOMParser().parseFromString(await res.text(), 'text/html');
            const other = doc.querySelectorAll('table.admin-table')[index];
            if (other) rows.push(...bodyRows(other, skip));
        }
        return rows;
    }

    // A list of inline edit forms: one row per form, one column per field label, in the order the fields first appear.
    function formListRows(forms) {
        const columns = [];
        const rows = forms.map(form => {
            const row = new Map();
            form.querySelectorAll(FIELDS).forEach(f => {
                const label = fieldLabel(f);
                if (!columns.includes(label)) columns.push(label);
                row.set(label, fieldValue(f));
            });
            return row;
        });
        return [columns, ...rows.map(r => columns.map(c => r.get(c) ?? ''))];
    }

    let exportable = 0;   // the number of lists on the page, so a page with several numbers its files

    function fileName(n) {
        const title = clean(document.title.split(' - ')[0]).replace(/[\\/:*?"<>|]/g, '-') || 'table';
        const d = new Date();
        const pad = x => String(x).padStart(2, '0');
        return `${title}${exportable > 1 ? '-' + n : ''}-${d.getFullYear()}${pad(d.getMonth() + 1)}${pad(d.getDate())}.csv`;
    }

    function download(lines, name) {
        const csv = lines.map(r => r.map(cell).join(',')).join('\r\n') + '\r\n';
        const blob = new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8' });
        const a = document.createElement('a');
        a.href = URL.createObjectURL(blob);
        a.download = name;
        document.body.appendChild(a);
        a.click();
        a.remove();
        setTimeout(() => URL.revokeObjectURL(a.href), 1000);
    }

    function addButton(card, before, single, build) {
        const n = ++exportable;
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'btn-admin btn-secondary btn-sm table-export-btn';
        button.textContent = LABEL;
        button.addEventListener('click', async () => {
            button.disabled = true;
            try {
                download(await build(), fileName(n));
            } catch {
                alert('تعذّر التصدير. أعد تحميل الصفحة وحاول مرة أخرى.');
            } finally {
                button.disabled = false;
            }
        });

        // Into the card's header when the card holds this list only; else in a small bar above the list.
        const header = card?.querySelector(':scope > .admin-card-header');
        if (header && single) {
            header.appendChild(button);
        } else {
            const bar = document.createElement('div');
            bar.className = 'table-export';
            bar.appendChild(button);
            before.before(bar);
        }
    }

    document.querySelectorAll('table.admin-table:not([data-export])').forEach(table => {
        if (!bodyRows(table, new Set()).length) return;
        const card = table.closest('.admin-card');
        const wrap = table.parentElement?.matches('.table-scroll, .table-responsive') ? table.parentElement : table;
        const single = card?.querySelectorAll('table.admin-table').length === 1;
        addButton(card, wrap, single, async () => {
            const { heads, skip } = skipped(table);
            return [heads.filter((_, i) => !skip.has(i)).map(th => clean(th.textContent)), ...await allRows(table, skip)];
        });
    });

    document.querySelectorAll('.admin-card').forEach(card => {
        const forms = [...card.querySelectorAll('form.row-form')];
        if (forms.length === 0 || card.querySelector('table.admin-table')) return;
        addButton(card, forms[0], true, async () => formListRows(forms));
    });
})();
