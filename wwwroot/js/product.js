
// xử lý drop dow kích cỡ , giá , lọc sản phẩm 
function toggleFilter(id) {
    document.querySelectorAll('.filter-dropdown').forEach(d => {
        if (d.id !== id) d.classList.remove('show');
    });
    document.getElementById(id).classList.toggle('show');
}
document.addEventListener('click', e => {
    if (!e.target.closest('.filter-item')) {
        document.querySelectorAll('.filter-dropdown').forEach(d => d.classList.remove('show'));
    }
});

(function () {
    const input = document.getElementById('searchInput');
    const clearBtn = document.getElementById('searchClear');
    const grid = document.getElementById('productGrid');
    const countEl = document.querySelector('.product-count');

    // Lấy categoryId hiện tại từ URL nếu có
    function getCategoryId() {
        const params = new URLSearchParams(window.location.search);
        return params.get('categoryId') || '';
    }

    let debounceTimer;

    input.addEventListener('input', function () {
        const q = this.value.trim();
        clearBtn.style.display = q ? 'flex' : 'none';

        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => doSearch(q), 350); // chờ 350ms sau khi dừng gõ
    });

    clearBtn.addEventListener('click', function () {
        input.value = '';
        clearBtn.style.display = 'none';
        doSearch('');
        input.focus();
    });

    function doSearch(keyword) {
        const catId = getCategoryId();
        const url = `/Product/SearchProducts?keyword=${encodeURIComponent(keyword)}&categoryId=${catId}`;

        grid.style.opacity = '0.4';
        grid.style.pointerEvents = 'none';

        fetch(url)
            .then(r => r.json())
            .then(data => {
                renderGrid(data);
                if (countEl) countEl.textContent = data.length + ' sản phẩm';
            })
            .catch(() => {
                grid.style.opacity = '1';
                grid.style.pointerEvents = '';
            });
    }

    function renderGrid(products) {
        grid.style.opacity = '1';
        grid.style.pointerEvents = '';

        if (products.length === 0) {
            grid.innerHTML = `
                <div style="grid-column:1/-1;text-align:center;padding:80px 0;
                            color:var(--light);font-size:14px;letter-spacing:1px;">
                    <i class="bi bi-box-seam"
                       style="font-size:40px;display:block;margin-bottom:16px;opacity:0.3"></i>
                    Không tìm thấy sản phẩm nào.
                </div>`;
            return;
        }

        grid.innerHTML = products.map((p, i) => `
            <div class="product-card" style="animation-delay:${i * 0.05}s">
                <div class="product-image">
                    <img src="${p.image}" alt="${p.productName}" loading="lazy" />
                    <div class="product-actions">
                        <button class="action-btn" title="Yêu thích">
                            <i class="bi bi-heart"></i>
                        </button>
                        <button class="action-btn" title="Thêm vào giỏ">
                            <i class="bi bi-bag-plus"></i>
                        </button>
                    </div>
                </div>
                <div class="product-info">
                    <div class="product-name">
                        <a href="#">${p.productName}</a>
                    </div>
                    <div class="product-price">
                        ${p.price.toLocaleString('vi-VN')}đ
                    </div>
                </div>
            </div>
        `).join('');
    }
}) ();
