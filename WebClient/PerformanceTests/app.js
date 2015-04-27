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
        FIVES.WebclientTestsuite.kiaraContext1 = KIARA.createContext();
        FIVES.WebclientTestsuite.kiaraService1 = "kiara/broker_client_2.json";
    }

    $(document).ready(main);
});
