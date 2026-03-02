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
let BASEURL = 'http://localhost:9144';
const ClickableOps = {};
// Make all 'class=k-clickable' page items create events
Array.from(document.getElementsByClassName('k-clickable')).forEach(nn => {
    nn.addEventListener('click', (evnt) => {
        const buttonOp = evnt.target.getAttribute('op');
        if (buttonOp && typeof (ClickableOps[buttonOp]) === 'function') {
            if (evnt.target)
                ClickableOps[buttonOp](evnt.target);
        }
        ;
    });
});
// Keep calling the update stats function every 500ms to keep the display updated
const timerIdStats = setInterval(() => UpdateAllStats(), 1000);
// Diagnostic test button
ClickableOps['diagTest'] = function (pTarget) {
    LogDebug('Diagnostic test operation invoked');
};
// Button to refetch the grid list from the viewer
ClickableOps['refetchGrids'] = function (pTarget) {
    LogDebug('Refetch grids operation invoked');
    FetchGridInfo();
};
// Button to do the login based on the form entries
ClickableOps['gridLogin'] = function (pTarget) {
    var first = document.getElementById('k-gridLogin-first').value;
    var last = document.getElementById('k-gridLogin-last').value;
    var password = document.getElementById('k-gridLogin-password').value;
    var startLoc = document.getElementById('k-gridLogin-startLoc').value;
    var grid = document.getElementById('k-gridLogin-gridSelect').value;
    LogDebug(`Logging in user ${first} ${last} to grid ${grid} with password`);
    var loginData = {
        FirstName: first,
        LastName: last,
        Password: password,
        StartLocation: startLoc,
        Grid: grid
    };
    fetch(BASEURL + '/api/Session/login', {
        method: 'POST',
        cache: 'no-cache',
        body: JSON.stringify(loginData),
        headers: { 'Content-Type': 'application/json' }
    })
        .then(response => {
        if (!response.ok) {
            LogDebug('Login failed: Network response was not ok');
        }
        return response.json();
    })
        .then(data => {
        LogDebug('Login data: ' + JSON.stringify(data));
        if (data["result"] == "success") {
            LogDebug('Login succeeded: ' + data["message"]);
        }
        else {
            LogDebug('Login failed: ' + data["message"]);
        }
    })
        .catch(error => {
        LogDebug('Login exception error: ' + error.message);
    });
};
// button to do the logout
ClickableOps['gridLogout'] = function (pTarget) {
    LogDebug('Do the logout');
    fetch(BASEURL + '/api/Session/logout', { method: 'POST', cache: 'no-cache' })
        .then(response => {
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        return;
    })
        .catch(error => {
        LogDebug('Logout exception error: ' + error.message);
    });
};
// button to force exit
ClickableOps['gridExit'] = function (pTarget) {
    LogDebug('Do the exit');
    fetch(BASEURL + '/api/Session/exit', { method: 'POST', cache: 'no-cache' })
        .then(response => {
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        return;
    })
        .catch(error => {
        LogDebug('Exit exception error: ' + error.message);
    });
};
// Fetch the list of grids from the viewer and populate the grid select box
function FetchGridInfo() {
    fetch(BASEURL + '/api/Session/login', { method: 'GET', cache: 'no-cache' })
        .then(response => {
        if (!response.ok) {
            LogDebug('Fetch grids failed: Network response was not ok');
            throw new Error('Network response was not ok');
        }
        return response.json();
    })
        .then(data => {
        // LogDebug('Grid data: ' + JSON.stringify(data));
        var selector = document.getElementById('k-gridLogin-gridSelect');
        selector.options.length = 0;
        for (let grid in data.grids) {
            let valu = data.grids[grid];
            let opt = document.createElement('option');
            opt.value = valu.GridNick;
            opt.text = valu.GridName;
            selector.add(opt);
        }
    })
        .catch(error => {
        LogDebug('Fetch grids exception error: ' + error.message);
    });
}
// ============================================================
function UpdateAllStats() {
    UpdateStats();
    UpdateCommStats();
    // Add more as needed
}
function UpdateStats() {
    fetch(BASEURL + '/api/stats', { method: 'GET', cache: 'no-cache' })
        .then(response => {
        if (!response.ok) {
            LogDebug('Fetch stats failed: Network response was not ok');
            DisplayNoStats();
        }
        return response.json();
    })
        .then((data) => {
        var displayArea = document.getElementById('k-stats');
        if (displayArea) {
            displayArea.innerHTML = '';
            displayArea.appendChild(FormatStatsData(data));
        }
    })
        .catch(error => {
        // LogDebug('Fetch stats exception error: ' + error.message);
        DisplayNoStats();
    });
}
function UpdateCommStats() {
    fetch(BASEURL + '/api/LLLP/stats', { method: 'GET', cache: 'no-cache' })
        .then(response => {
        if (!response.ok) {
            LogDebug('Fetch Comm stats failed: Network response was not ok');
            DisplayNoCommStats();
        }
        return response.json();
    })
        .then((data) => {
        var displayArea = document.getElementById('k-statsComm');
        if (displayArea) {
            displayArea.innerHTML = '';
            displayArea.appendChild(FormatLLLPStatsData(data));
        }
    })
        .catch(error => {
        // LogDebug('Fetch Comm stats exception error: ' + error.message);
        DisplayNoCommStats();
    });
}
function DisplayNoStats() {
    var displayArea = document.getElementById('k-stats');
    if (displayArea) {
        displayArea.innerHTML = '';
        displayArea.appendChild(MakeTextElement("No stats"));
    }
}
function DisplayNoCommStats() {
    var displayArea = document.getElementById('k-statsComm');
    if (displayArea) {
        displayArea.innerHTML = '';
        displayArea.appendChild(MakeTextElement("No comm stats"));
    }
}
// ============================================================
function FormatStatsData(data) {
    var allStats = MakeElement('div', 'div-stats-all');
    var tbl = MakeElement('table', 'table-stats-header');
    tbl.appendChild(MakeStatInfoRow("TimeStamp", data.timestamp));
    tbl.appendChild(MakeStatInfoRow("Is Connected", data.isconnected ? "Yes" : "No"));
    tbl.appendChild(MakeStatInfoRow("Is Logged In", data.isloggedin ? "Yes" : "No"));
    allStats.appendChild(tbl);
    allStats.appendChild(MakeStatsWorld(data.world));
    allStats.appendChild(MakeSectionHeader("Work Queues"));
    allStats.appendChild(MakeStatsWorkQueue(data.workqueues));
    allStats.appendChild(MakeStatsComponents(data.components));
    return allStats;
    // var preArea = MakeElement('pre');
    // preArea.textContent = JSON.stringify(data, null, 2);
    // return preArea;
}
function MakeStatsWorld(world) {
    if (!world || !world.Regions) {
        var noDataDiv = MakeElement('div', 'div-stats-world');
        noDataDiv.appendChild(MakeSectionHeader("World - No data"));
        return noDataDiv;
    }
    var worldDiv = MakeElement('div', 'div-stats-world');
    worldDiv.appendChild(MakeSectionHeader(`World - ${world.RegionCount} regions`));
    for (let region in world.Regions) {
        let info = world.Regions[region];
        var regionDiv = MakeElement('div', 'div-stats-region');
        regionDiv.appendChild(MakeSectionHeader(`Region: ${info.Name} - ${info.EntityCount} entities`));
        /*
        var entityTable = MakeElement('table', 'table-stats-entities');
        entityTable.appendChild(MakeHeaderRow(['Name', 'LGID', 'Classification', 'Containing Entity', 'Components']));
        for (let entity of info.Entities) {
            var row = MakeElement('tr');
            row.appendChild(MakeTDElement(entity.Name));
            row.appendChild(MakeTDElement(entity.LGID));
            row.appendChild(MakeTDElement(entity.Classification));
            row.appendChild(MakeTDElement(entity.ContainingEntity));
            row.appendChild(MakeTDElement(entity.Components.join(", ")));
            entityTable.appendChild(row);
        }
        regionDiv.appendChild(entityTable);
        */
        worldDiv.appendChild(regionDiv);
    }
    return worldDiv;
}
function MakeStatsWorkQueue(workQueues) {
    if (!workQueues) {
        var noDataDiv = MakeElement('div', 'div-stats-workqueue');
        noDataDiv.appendChild(MakeSectionHeader("Work Queues - No data"));
        return noDataDiv;
    }
    var wqTable = MakeElement('table', 'table-stats-workqueue');
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
function MakeStatsComponents(components) {
    if (!components || !components.ComponentTypes) {
        var noDataDiv = MakeElement('div', 'div-stats-components');
        noDataDiv.appendChild(MakeSectionHeader("Components - No data"));
        return noDataDiv;
    }
    var compDiv = MakeElement('div', 'div-stats-components');
    compDiv.appendChild(MakeSectionHeader(`Components - ${components.ComponentTypes.length} types`));
    var compTable = MakeElement('table', 'table-stats-components');
    compTable.appendChild(MakeHeaderRow(['Component Type', 'Count']));
    for (let type of components.ComponentTypes) {
        var row = MakeElement('tr');
        row.appendChild(MakeTDElement(type));
        row.appendChild(MakeTDElement(components.ComponentCounts[type]?.toString() ?? "0"));
        compTable.appendChild(row);
    }
    compDiv.appendChild(compTable);
    return compDiv;
}
// ============================================================
function FormatLLLPStatsData(data) {
    var allStats = MakeElement('div', 'div-lllp-stats-all');
    allStats.appendChild(MakeSectionHeader("LLLP Communication Stats"));
    allStats.appendChild(MakeLLLPStatsHeader(data));
    allStats.appendChild(MakeLLLPStatsComm(data.commstats));
    allStats.appendChild(MakeLLLPStatsAvatar(data.avatar));
    return allStats;
}
function MakeLLLPStatsHeader(data) {
    var tbl = MakeElement('table', 'table-lllp-stats-header');
    tbl.appendChild(MakeStatInfoRow("TimeStamp", data.timestamp));
    tbl.appendChild(MakeStatInfoRow("Comm Provider", data.commprovider));
    tbl.appendChild(MakeStatInfoRow("Is Connected", data.isconnected ? "Yes" : "No"));
    tbl.appendChild(MakeStatInfoRow("Is Logged In", data.isloggedin ? "Yes" : "No"));
    tbl.appendChild(MakeStatInfoRow("Current Grid", data.currentgrid));
    tbl.appendChild(MakeStatInfoRow("Current Sim", data.currentsim));
    return tbl;
}
function MakeStatInfoRow(pDisplayName, pDisplayValue) {
    var row = MakeElement('tr');
    var cellKey = MakeElement('td');
    cellKey.textContent = pDisplayName;
    var cellValue = MakeElement('td');
    cellValue.textContent = pDisplayValue;
    row.appendChild(cellKey);
    row.appendChild(cellValue);
    return row;
}
function MakeLLLPStatsComm(commStats) {
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
function MakeLLLPStatsAvatar(avatarInfo) {
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
function MakeElement(tag, className) {
    const elem = document.createElement(tag);
    if (className)
        elem.classList.add(className);
    return elem;
}
function MakeTextElement(text) {
    const elem = document.createTextNode(text);
    elem.textContent = text;
    return elem;
}
function MakeSectionHeader(text) {
    const header = MakeElement('h3');
    header.textContent = text;
    return header;
}
// Create a td element containing text with an optional class name
function MakeTDElement(text, className) {
    const elem = document.createElement('td');
    elem.textContent = text;
    if (className)
        elem.classList.add(className);
    return elem;
}
// Given an array of headings, create the th headers for a table row
function MakeHeaderRow(pHeaders) {
    var row = MakeElement('tr');
    for (let header of pHeaders) {
        var cell = MakeElement('th');
        cell.textContent = header;
        row.appendChild(cell);
    }
    return row;
}
//# sourceMappingURL=diag.js.map