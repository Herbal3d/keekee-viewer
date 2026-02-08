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

// Keep calling the update stats function every 500ms to keep the display updated
const timerIdStats = setInterval(() => UpdateAllStats(), 500);

// Diagnostic test button
ClickableOps['diagTest'] = function(pTarget: EventTarget) : void {
    LogDebug('Diagnostic test operation invoked');
};
// Button to refetch the grid list from the viewer
ClickableOps['refetchGrids'] = function(pTarget: EventTarget) : void {
    LogDebug('Refetch grids operation invoked');
    FetchGridInfo();
};
// Button to do the login based on the form entries
ClickableOps['gridLogin'] = function(pTarget: EventTarget) : void {
    var first = (document.getElementById('k-gridLogin-first') as HTMLInputElement).value;
    var last = (document.getElementById('k-gridLogin-last') as HTMLInputElement).value;
    var password = (document.getElementById('k-gridLogin-password') as HTMLInputElement).value;
    var startLoc = (document.getElementById('k-gridLogin-startLoc') as HTMLInputElement).value;
    var grid = (document.getElementById('k-gridLogin-gridSelect') as HTMLSelectElement).value;
    
    LogDebug(`Logging in user ${first} ${last} to grid ${grid} with password`);

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
            LogDebug('Login failed: Network response was not ok');
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
// button to do the logout
ClickableOps['gridLogout'] = function(pTarget: EventTarget) :void {
    LogDebug('Do the logout');
    fetch( BASEURL + '/api/LLLP/logout', { method: 'POST', cache: 'no-cache' } )
    .then( response => {
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        return;
    })
    .catch( error => {
        LogDebug('Logout exception error: ' + error.message);
    });
};
// button to force exit
ClickableOps['gridExit'] = function(pTarget: EventTarget) : void {
    LogDebug('Do the exit');
    fetch( BASEURL + '/api/LLLP/exit', { method: 'POST', cache: 'no-cache' } )
    .then( response => {
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        return;
    })
    .catch( error => {
        LogDebug('Exit exception error: ' + error.message);
    });
};

// ============================================================
interface GridInfo {
    GridNick: string;
    GridName: string;
    LoginURI: string;
}
interface GridsInfo {
    grids: Array<GridInfo>;
}

// Fetch the list of grids from the viewer and populate the grid select box
function FetchGridInfo() : void{
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

// ============================================================
function UpdateAllStats() : void {
    UpdateStats();
    UpdateCommStats();
    // Add more as needed
}

function UpdateStats() : void {
    fetch( BASEURL + '/api/stats', { method: 'GET', cache: 'no-cache' } )
    .then( response => {
        if (!response.ok) {
            DisplayNoStats();
            throw new Error('Network response was not ok');
        }
        return response.json();
    })
    .then( (data: StatsData) => {
        var displayArea = document.getElementById('k-stats');
        if (displayArea) {
            displayArea.innerHTML = '';
            displayArea.appendChild(FormatStatsData(data));
        }
    })
    .catch( error => {
        // LogDebug('Fetch Comm stats exception error: ' + error.message);
        DisplayNoStats();
    });
}
function UpdateCommStats() : void {
    fetch( BASEURL + '/api/LLLP/stats', { method: 'GET', cache: 'no-cache' } )
    .then( response => {
        if (!response.ok) {
            DisplayNoCommStats();
            throw new Error('Network response was not ok');
        }
        return response.json();
    })
    .then( (data: LLLPStatsData) => {
        var displayArea = document.getElementById('k-statsComm');
        if (displayArea) {
            displayArea.innerHTML = '';
            displayArea.appendChild(FormatLLLPStatsData(data));
        }
    })
    .catch( error => {
        // LogDebug('Fetch Comm stats exception error: ' + error.message);
        DisplayNoCommStats();
    });
}
function DisplayNoStats() : void {
    var displayArea = document.getElementById('k-stats');
    if (displayArea) {
        displayArea.innerHTML = '';
        displayArea.appendChild(MakeTextElement("No stats"));
    }
}
function DisplayNoCommStats() : void {
    var displayArea = document.getElementById('k-statsComm');
    if (displayArea) {
        displayArea.innerHTML = '';
        displayArea.appendChild(MakeTextElement("No comm stats"));
    }
}
// ============================================================
function FormatStatsData(data: StatsData) : HTMLElement {
    var allStats = MakeElement('div', 'div-stats-all');
    var tbl = MakeElement('table', 'table-stats-header');
    tbl.appendChild(MakeStatInfoRow("TimeStamp", data.timestamp));
    tbl.appendChild(MakeStatInfoRow("Is Connected", data.isconnected ? "Yes" : "No"));
    tbl.appendChild(MakeStatInfoRow("Is Logged In", data.isloggedin ? "Yes" : "No"));
    allStats.appendChild(tbl);
    allStats.appendChild(MakeStatsWorkQueue(data.workqueues));
    return allStats;
    // var preArea = MakeElement('pre');
    // preArea.textContent = JSON.stringify(data, null, 2);
    // return preArea;
}
function MakeStatsWorkQueue(workQueues: { [index: number]: WorkQueueInfo }) : HTMLElement {
    var wqTable = MakeElement('table');
    wqTable.appendChild(MakeHeaderRow(['Name', 'Total', 'Current', 'Later', 'Active']));
    for (let wq in workQueues) {
        let info = workQueues[wq];
        var row = MakeElement('tr');
        row.appendChild(MakeTDElement(info.Name));
        row.appendChild(MakeTDElement(info.Total.toString()));
        row.appendChild(MakeTDElement(info.Current.toString()));
        row.appendChild(MakeTDElement(info.Later.toString()));
        row.appendChild(MakeTDElement(info.Active.toString()));
        wqTable.appendChild(row);
    }
    return wqTable;
}
// ============================================================
function FormatLLLPStatsData(data: LLLPStatsData) : HTMLElement {
    var allStats = MakeElement('div', 'div-lllp-stats-all');
    allStats.appendChild(MakeLLLPStatsHeader(data));
    allStats.appendChild(MakeLLLPStatsComm(data.commstats));
    allStats.appendChild(MakeLLLPStatsAvatar(data.avatar));
    return allStats;
}
function MakeLLLPStatsHeader(data: LLLPStatsData) : HTMLElement {
    var tbl = MakeElement('table', 'table-lllp-stats-header');
    tbl.appendChild(MakeStatInfoRow("TimeStamp", data.timestamp));
    tbl.appendChild(MakeStatInfoRow("Comm Provider", data.commprovider));
    tbl.appendChild(MakeStatInfoRow("Is Connected", data.isconnected ? "Yes" : "No"));
    tbl.appendChild(MakeStatInfoRow("Is Logged In", data.isloggedin ? "Yes" : "No"));
    tbl.appendChild(MakeStatInfoRow("Current Grid", data.currentgrid));
    tbl.appendChild(MakeStatInfoRow("Current Sim", data.currentsim));
    return tbl;
}
function MakeStatInfoRow(pDisplayName: string, pDisplayValue: string) : HTMLElement {
    var row = MakeElement('tr');
    var cellKey = MakeElement('td');
    cellKey.textContent = pDisplayName;
    var cellValue = MakeElement('td');
    cellValue.textContent = pDisplayValue;
    row.appendChild(cellKey);
    row.appendChild(cellValue);
    return row;
}
function MakeLLLPStatsComm(commStats: { [index: number]: CommStatInfo }) : HTMLElement {
    var commTable = MakeElement('table', 'table-stats-comm');
    commTable.appendChild(MakeHeaderRow(['Name', 'Value', 'Description']));
    for (let stat in commStats) {
        let info = commStats[stat];
        var row = MakeElement('tr');
        row.appendChild(MakeTDElement(info.Name));
        row.appendChild(MakeTDElement(`${info.Value} ${info.Unit}`));
        row.appendChild(MakeTDElement(info.Description));
        commTable.appendChild(row);
    }
    return commTable;
}
function MakeLLLPStatsAvatar(avatarInfo: { [index: number]: AvatarInfo }) : HTMLElement {
    var avatarTable = MakeElement('table');
    avatarTable.appendChild(MakeHeaderRow(['Display Name', 'First Name', 'Last Name', 'Global Pos', 'Local Pos']));
    for (let av in avatarInfo) {
        let info = avatarInfo[av];
        var row = MakeElement('tr');
        row.appendChild(MakeTDElement(info.displayname));
        row.appendChild(MakeTDElement(info.first));
        row.appendChild(MakeTDElement(info.last));
        row.appendChild(MakeTDElement(info.globalPos));
        row.appendChild(MakeTDElement(info.localPos));
        avatarTable.appendChild(row);
    }
    return avatarTable;
}

// ============================================================
function MakeElement(tag: string, className?: string): HTMLElement {
    const elem = document.createElement(tag);
    if (className) elem.classList.add(className);
    return elem;
}
function MakeTextElement(text: string): HTMLElement {
    const elem = document.createTextNode(text) as unknown as HTMLElement;
    elem.textContent = text;
    return elem;
}   
// Create a td element containing text with an optional class name
function MakeTDElement(text: string, className?: string): HTMLElement {
    const elem = document.createElement('td');
    elem.textContent = text;
    if (className) elem.classList.add(className);
    return elem;
}
// Given an array of headings, create the th headers for a table row
function MakeHeaderRow(pHeaders: string[]) : HTMLElement {
    var row = MakeElement('tr');
    for (let header of pHeaders) {
        var cell = MakeElement('th');
        cell.textContent = header;
        row.appendChild(cell);
    }
    return row;
}
// ============================================================
interface WorkQueueInfo {
    "Name": string;
    "Total": number;
    "Current": number;
    "Later": number;
    "Active": number;
}
interface StatsData {
    "status": string;
    "timestamp": string;
    "commprovider": string;
    "isconnected": boolean;
    "isloggedin": boolean;
    "workqueues": {
        [ index: number]: WorkQueueInfo
    },
}

// ============================================================
interface AvatarInfo {
    "first": string;
    "last": string;
    "displayname": string;
    "globalPos": string;
    "localPos": string;
    "globalx": number;
    "globaly": number;
    "globalz": number;
    "x": number;
    "y": number;
    "z": number;
}
interface CommStatInfo {
    "Name": string;
    "Description": string;
    "Unit": string;
    "Value": number;
}
// The data returned by /api/LLLP/status.
interface LLLPStatsData {
    "status": string;
    "timestamp": string;
    "commprovider": string;
    "isconnected": boolean;
    "isloggedin": boolean;
    "currentgrid": string;
    "currentsim": string;
    "avatar": {
        [index: number]: AvatarInfo
    },
    "commconfig": {
        [key: string]: string;
    },
    "commstats": {
        [index: number]: CommStatInfo
    },
    "possiblegrids": {
        [index: number]: string;
    },
    [key: string]: any; // Allow for additional properties
}
// Specify the column names or the fields to extract to fill the columm.
type ColumnSpec = string[];

interface TableData {
    [rowKey: string]: { [colKey: string]: any };
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

