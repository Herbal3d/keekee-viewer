// Copyright 2025 Robert Adams
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

import { LogDebug } from "./DebugLog.js";

let BASEURL='http://localhost:9144'

// Clickable operations mapping
type ClickOperation = ( pTarget: EventTarget ) => void;
const ClickableOps: { [key: string]: ClickOperation } = {};

// Make all 'class=b-clickable' page items create events
Array.from(document.getElementsByClassName('k-clickable')).forEach( nn => {
    nn.addEventListener('click', (evnt: Event) => {
        const buttonOp = (evnt.target as HTMLElement).getAttribute('op');
        if (buttonOp && typeof(ClickableOps[buttonOp]) === 'function') {
            if (evnt.target)
                ClickableOps[buttonOp](evnt.target);
        };
    });
});

// Diagnostic test button
ClickableOps['diagTest'] = function(pTarget: EventTarget) {
    LogDebug('Diagnostic test operation invoked');
};
// Button to refetch the grid list from the viewer
ClickableOps['refetchGrids'] = function(pTarget: EventTarget) {
    LogDebug('Refetch grids operation invoked');
    FetchGridInfo();
};
// Button to do the login based on the form entries
ClickableOps['gridLogin'] = function(pTarget: EventTarget) {
    LogDebug('Do the login');
    var first = (document.getElementById('k-gridLogin-first') as HTMLInputElement).value;
    var last = (document.getElementById('k-gridLogin-last') as HTMLInputElement).value;
    var password = (document.getElementById('k-gridLogin-password') as HTMLInputElement).value;
    var startLoc = (document.getElementById('k-gridLogin-startLoc') as HTMLInputElement).value;
    var grid = (document.getElementById('k-gridLogin-gridSelect') as HTMLSelectElement).value;
    
    LogDebug(`Logging in user ${first} ${last} to grid ${grid} with password ${password}`);

    var loginData = {
        FirstName: first,
        LastName: last,
        Password: password,
        StartLocation: startLoc,
        Grid: grid
    };

    fetch( BASEURL + '/api/LLLP/login', {
                method: 'POST',
                cache: 'no-cache',
                body: JSON.stringify(loginData),
                headers: { 'Content-Type': 'application/json' }
    } )
    .then( response => {
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        return response.json();
    })
    .then( data => {
        LogDebug('Login data: ' + JSON.stringify(data));
        if (data["result"] == "success") {
            LogDebug('Login succeeded: ' + data["message"]);
        }
        else {
            LogDebug('Login failed: ' + data["message"]);
        }
    })
    .catch( error => {
        LogDebug('Login exception error: ' + error.message);
    });
};

interface GridInfo {
    GridNick: string;
    GridName: string;
    LoginURI: string;
}
interface GridsInfo {
    grids: Array<GridInfo>;
}

// Fetch the list of grids from the viewer and populate the grid select box
function FetchGridInfo() {
    fetch( BASEURL + '/api/LLLP/login', { method: 'GET', cache: 'no-cache' } )
    .then( response => {
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        return response.json();
    })
    .then( data => {
        LogDebug('Grid data: ' + JSON.stringify(data));
        var selector = (document.getElementById('k-gridLogin-gridSelect') as HTMLSelectElement);
        selector.options.length = 0;
        for (let grid in data.grids) {
            let valu = data.grids[grid];
            let opt = document.createElement('option');
            opt.value = valu.GridNick;
            opt.text = valu.GridName;
            selector.add(opt);
        }
    })
    .catch( error => {
        LogDebug('Fetch grids exception error: ' + error.message);
    });
}

/*
<script type="text/javascript">
$(document).ready(function() {
    // setup form to post info when 'Login' is pressed
    $("#k-LoginForm").submit(SendLoginRequest);

    // Major divisions in the content accordioning
    $('.k-Section').hide();
    $('#k-Login').show('slow');
    $('.k-SectionHeader').click(function() {
        $(this).next().slideToggle('slow');
        return false;
    });

    // Start the timed functions
    TimerLoginStuff(true);
    TimerDataStuff();
});

// Login works by polling the viewer for the status of the user.
// If the user is logged in, we display this status and the user location.
// If not logged in, the user filled in fields and pressed 'login'
// which POSTs parameters to the viewer which change the user's
// login state.
// var BASEURL='http://127.0.0.1:9144';
var BASEURL='';
var loginTimerHandle;
// poll for the user's date. Initialize form if first GET.
function TimerLoginStuff(firstTime) {
    fetch( BASEURL + '/api/LLLP/status', { method: 'GET', cache: 'no-cache' } )
    .then( response => {
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        return response.json();
    })
    .then( data => {
        LogDebug('Login data: ' + JSON.stringify(data));
        data["loginstate"] = (data["isloggedin"]) ? "loggedin" : "loggedout";

        if (firstTime) InitializeLoginForm(data);
        UpdateLoginArea(data);
    })
    .catch( error => {
        UpdateLoginArea( {
            msg: "Lost connection to viewer",
            loginstate: "loggedout"
        });
    });
            
    loginTimerHandle = setTimeout(() => TimerLoginStuff(false), 3000);
};

// given a set of login status parameters, update login status area
function UpdateLoginArea(data) {
    $('#k-CurrentLogin td').empty();
    if (data['first'] != undefined) {
        $('#k-CurrentLogin td:nth-child(1)').append(data.first.value);
        $('#k-CurrentLogin td:nth-child(2)').append(data.last.value);
        $('#k-CurrentLogin td:nth-child(3)').append(data.currentgrid.value);
        $('#k-CurrentLogin td:nth-child(4)').append(data.currentsim.value);
        $('#k-CurrentLogin td:nth-child(5)').append(data.positionx.value);
        $('#k-CurrentLogin td:nth-child(6)').append(data.positiony.value);
        $('#k-CurrentLogin td:nth-child(7)').append(data.positionz.value);
    }

    $('#k-LoginMessage').empty();
    if (data.msg != undefined)
        $('#k-LoginMessage').append(data.msg.value);

    if(data.loginstate.value == 'logout') {
        $("#k-LoginForm").slideDown('slow');
    }
    else {
        $("#k-LoginForm").slideUp('slow');
    }
}

// Called the first time so we can pre-populate the form
function InitializeLoginForm(data) {
    if (data['first'] != undefined) {
        $('#k-LoginForm input[name="LOGINFIRST"]').attr('value', data.first?.value ?? "");
        $('#k-LoginForm input[name="LOGINLAST"]').attr('value', data.last?.value ?? "");
        $('#k-LoginForm input[name="LOGINSIM"]').attr('value', data.currentsim?.value ?? "");
    }
    var selector = $('#k-LoginForm select[name="LOGINGRID"]');
    selector.empty();
    for (grid in data.possiblegrids.value) {
        var valu = data.possiblegrids.value[grid];
        selector.append('<option value="' + valu + '">' + valu + '</option>');
    }
}

// Called by login form 'submit' click
// Post the user's parameters to the viewer to login the user
function SendLoginRequest() {
    $.post(BASEURL + "/api/LLLP/connect/login", $('#k-LoginForm').serializeArray());
    return false;
}

// One of the sections is viewer statistics. Poll for the data.
var statTimerHandle;
var graphFPS;
var lastFPS = 10;
var xxThru = 0;
function TimerDataStuff() {
    statTimerHandle = setTimeout(() => TimerStatDisplay(), 2000);
}

// called by timer to fetch and display statistic information
function TimerStatDisplay() {
    if ($('#k-QueueStats').is(':visible')) {
        $.getJSON(BASEURL+'/api/stats/workQueues', function(data, status) {
            if (status == 'success') {
                BuildBasicTable('#k-QueueStats', data, false, false);
            }
        });
        $.getJSON(BASEURL+'/api/stats/Renderer/detailStats', function(data, status) {
            if (status == 'success') {
                BuildBasicTable('#k-RendererStats', data, true, false);
            }
        });
        $.getJSON(BASEURL+'/api/stats/Renderer/ogreStats', function(data, status) {
            if (status == 'success') {
                BuildBasicTable('#k-OgreStats', data, false, false, true);
                if (typeof(graphFPS) == 'undefined') {
                    graphFPS = new TrendData(100);
                    graphFPS.Format = {type:'bar', width:'auto',height:'20px'};
                                
                }
                var thisFPS = data['framespersecond']['value'];
                if (thisFPS > (lastFPS * 2)) thisFPS = lastFPS * 2;
                lastFPS = (lastFPS + thisFPS) / 2;
                graphFPS.AddPoint(thisFPS);
                graphFPS.UpdateDisplay('#k-OgreStats-framespersecond-Display');
            }
        });
        $.getJSON(BASEURL+'/api/stats/Comm/stats', function(data, status) {
            if (status == 'success') {
                BuildBasicTable('#k-CommStats', data, false, false);
            }
        });
    }
    statTimerHandle = setTimeout(() => TimerStatDisplay(), 2000);
};

</script>
*/

