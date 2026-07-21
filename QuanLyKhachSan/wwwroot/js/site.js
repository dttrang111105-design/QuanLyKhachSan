document.addEventListener("DOMContentLoaded", function () {
    const navbar = document.getElementById("mainNavbar");
    const scrollTopButton = document.querySelector(".scroll-top");
    const menuElement = document.getElementById("mainMenu");

    function updatePageState() {
        const scrollPosition = window.scrollY;

        // Đổi nền navbar khi cuộn trang chủ.
        if (navbar && navbar.classList.contains("navbar-home")) {
            navbar.classList.toggle(
                "navbar-scrolled",
                scrollPosition > 40
            );
        }

        // Hiện nút cuộn lên đầu.
        if (scrollTopButton) {
            scrollTopButton.classList.toggle(
                "show",
                scrollPosition > 350
            );
        }
    }

    updatePageState();

    window.addEventListener("scroll", updatePageState, {
        passive: true
    });

    // Cuộn lên đầu trang.
    if (scrollTopButton) {
        scrollTopButton.addEventListener("click", function () {
            window.scrollTo({
                top: 0,
                behavior: "smooth"
            });
        });
    }

    // Đóng menu điện thoại sau khi chọn liên kết.
    if (menuElement) {
        const menuLinks =
            menuElement.querySelectorAll(".nav-link");

        menuLinks.forEach(function (link) {
            link.addEventListener("click", function () {
                if (window.innerWidth < 992) {
                    const menuInstance =
                        bootstrap.Collapse.getInstance(menuElement);

                    if (menuInstance) {
                        menuInstance.hide();
                    }
                }
            });
        });
    }

    // Kiểm tra ngày tìm kiếm tại trang chủ.
    const checkInInput =
        document.getElementById("homeCheckIn");

    const checkOutInput =
        document.getElementById("homeCheckOut");

    if (checkInInput && checkOutInput) {
        checkInInput.addEventListener("change", function () {
            if (!checkInInput.value) {
                return;
            }

            const checkInDate =
                new Date(checkInInput.value + "T00:00:00");

            checkInDate.setDate(checkInDate.getDate() + 1);

            const nextDay =
                checkInDate.toISOString().split("T")[0];

            checkOutInput.min = nextDay;

            if (
                !checkOutInput.value ||
                checkOutInput.value <= checkInInput.value
            ) {
                checkOutInput.value = nextDay;
            }
        });
    }
});