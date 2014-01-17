require.config({
    paths: {
        'jquery' : '../scripts/lib/jquery',
        'kiara' : '../scripts/lib/kiara',
        'websocket-json' : '../scripts/lib/websocket-json'
    }
});

var FIVES = FIVES || {};
FIVES.WebclientTestsuite = FIVES.WebclientTestsuite || {};

requirejs(['kiara', 'jquery', 'websocket-json'],
function(KIARA, $) {

    function main() {
        FIVES.WebclientTestsuite.kiaraContext = KIARA.createContext();
        FIVES.WebclientTestsuite.kiaraService = "kiara/fives.json";
    }

    $(document).ready(main);
});
