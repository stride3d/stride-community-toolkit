const app = {
    iconLinks: [
        {
            icon: 'github',
            href: 'https://github.com/stride3d/stride-community-toolkit',
            title: 'Stride Community Toolkit GitHub'
        },
        {
            icon: 'discord',
            href: 'https://discord.gg/f6aerfE',
            title: 'Discord'
        },
        {
            icon: 'twitter',
            href: 'https://twitter.com/stridedotnet',
            title: 'Twitter'
        }
    ],
    start: function () { }
};

export default app;

document.addEventListener("DOMContentLoaded", () => app.start());