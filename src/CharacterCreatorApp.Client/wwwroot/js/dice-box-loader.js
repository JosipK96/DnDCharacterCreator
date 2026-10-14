(async function () {
    try {
        console.log("Loading dice-box module...");

        const module = await import('/lib/dice-box/dice-box.es.js');
        const DiceBox = module.default;

        let diceBox = null;

        window.initFantasticDice = async function () {
            console.log("initFantasticDice called");

            // Always destroy existing instance first
            if (diceBox) {
                console.log("Destroying existing DiceBox...");
                try {
                    diceBox.clear();
                    // Give it time to clean up
                    await new Promise(resolve => setTimeout(resolve, 100));
                } catch (e) {
                    console.warn("Error clearing old dice:", e);
                }
                diceBox = null;
            }

            try {
                const container = document.querySelector('#dice-canvas');

                // Clear any existing canvas
                while (container.firstChild) {
                    container.removeChild(container.firstChild);
                }

                const width = window.innerWidth;
                const height = window.innerHeight;

                console.log('Viewport:', width, 'x', height);

                container.style.width = width + 'px';
                container.style.height = height + 'px';

                diceBox = new DiceBox("#dice-canvas", {
                    assetPath: "/lib/dice-box/assets/",
                    theme: "default",
                    themeColor: "#e5e7eb",
                    scale: 5,
                    gravity: 2,
                    mass: 1,
                    friction: 0.8,
                    restitution: 0.3,
                    spinForce: 5,
                    throwForce: 4,
                    startingHeight: 8,
                    settleTimeout: 5000,
                    enableShadows: true,
                    lightIntensity: 2,
                    offscreen: false
                });

                await diceBox.init();

                setTimeout(() => {
                    const canvas = document.querySelector('#dice-canvas canvas');
                    if (canvas) {
                        canvas.width = width;
                        canvas.height = height;
                        canvas.style.width = width + 'px';
                        canvas.style.height = height + 'px';
                        canvas.style.position = 'absolute';
                        canvas.style.top = '0';
                        canvas.style.left = '0';

                        console.log('✅ Canvas resized:', canvas.width, 'x', canvas.height);
                        window.dispatchEvent(new Event('resize'));
                    }
                }, 200);

                console.log("✅ Dice initialized!");

            } catch (error) {
                console.error("❌ DiceBox init error:", error);
                throw error;
            }
        };

        window.rollFantasticDice = async function (diceNotation) {
            console.log("🎲 Rolling:", diceNotation);

            if (!diceBox) {
                throw new Error("DiceBox not initialized!");
            }

            return new Promise((resolve, reject) => {
                diceBox.roll(diceNotation).then((results) => {
                    const values = results.map(r => r.value);
                    console.log("✅ Roll complete:", values);
                    resolve(values);
                }).catch(error => {
                    console.error("❌ Roll error:", error);
                    reject(error);
                });
            });
        };

        console.log("✅ Dice functions ready!");

    } catch (error) {
        console.error("❌ Failed to load dice-box:", error);
    }
})();