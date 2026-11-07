// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("table.tf-paginated-table").forEach((table) => {
        const body = table.tBodies[0];
        if (!body) return;

        const rows = Array.from(body.rows).filter((row) => !row.querySelector("td[colspan]"));
        if (rows.length <= 10) return;

        const wrapper = table.closest(".table-responsive");
        if (!wrapper) return;
        wrapper.classList.add("tf-table-scroll");

        let pageSize = 10;
        let page = 0;
        const pager = document.createElement("div");
        pager.className = "tf-table-pager";

        const summary = document.createElement("span");
        summary.className = "tf-table-count";

        const sizeLabel = document.createElement("label");
        sizeLabel.className = "tf-page-size-label";
        sizeLabel.textContent = "Số dòng";
        const sizeSelect = document.createElement("select");
        sizeSelect.className = "form-select form-select-sm tf-page-size";
        sizeSelect.setAttribute("aria-label", "Số dòng trên mỗi trang");
        [10, 20, 50].forEach((size) => {
            const option = document.createElement("option");
            option.value = String(size);
            option.textContent = String(size);
            sizeSelect.append(option);
        });
        sizeLabel.append(sizeSelect);

        const controls = document.createElement("div");
        controls.className = "tf-page-controls";
        const previous = document.createElement("button");
        previous.type = "button";
        previous.className = "btn btn-sm btn-outline-secondary";
        previous.textContent = "‹ Trước";
        const next = document.createElement("button");
        next.type = "button";
        next.className = "btn btn-sm btn-outline-secondary";
        next.textContent = "Tiếp ›";
        const pageIndicator = document.createElement("span");
        pageIndicator.className = "tf-page-indicator";
        controls.append(previous, pageIndicator, next);
        pager.append(summary, sizeLabel, controls);
        wrapper.insertAdjacentElement("afterend", pager);

        const render = () => {
            const pageCount = Math.ceil(rows.length / pageSize);
            page = Math.min(page, pageCount - 1);
            const start = page * pageSize;
            const end = Math.min(start + pageSize, rows.length);
            rows.forEach((row, index) => { row.hidden = index < start || index >= end; });
            summary.textContent = `Hiển thị ${start + 1}–${end} / ${rows.length} dòng`;
            pageIndicator.textContent = `${page + 1} / ${pageCount}`;
            previous.disabled = page === 0;
            next.disabled = page >= pageCount - 1;
        };

        sizeSelect.addEventListener("change", () => {
            pageSize = Number(sizeSelect.value);
            page = 0;
            render();
        });
        previous.addEventListener("click", () => { page -= 1; render(); });
        next.addEventListener("click", () => { page += 1; render(); });
        render();
    });
});
