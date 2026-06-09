window.mediAlertNotifications = {
    requestPermission: async () => {
        if (!("Notification" in window)) {
            console.log("This browser does not support desktop notification");
            return false;
        }

        if (Notification.permission === "granted") {
            return true;
        }

        if (Notification.permission !== "denied") {
            const permission = await Notification.requestPermission();
            return permission === "granted";
        }

        return false;
    },

    scheduleNotification: (title, options, delayMs) => {
        setTimeout(() => {
            if (Notification.permission === "granted") {
                new Notification(title, options);
            }
        }, delayMs);
    },

    showNotification: (title, options) => {
        if (Notification.permission === "granted") {
            new Notification(title, options);
        }
    }
};
