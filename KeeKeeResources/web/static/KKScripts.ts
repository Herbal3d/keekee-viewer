// Various common JS routines written for KeeKee
// Copyright 2026, Robert Adams
// Licensed under Mozilla Public License 2.0
// See LICENSE file in top directory for details.

// ===========================================
// Data format for table building:
//   {rowname: { col1: val, col2: val; ...}, ...)
interface TableData {
    [rowName: string]: {
        [colName: string]: string | number | boolean | null;
    };
}

// ===========================================
// Build a string for a table to hold the passed data.
// We presume the data is in the form:
//   {rowname: { col1: val, col2: val; ...}, ...)
// 'sect' is the section of the document to build the table in
// 'data' is the data to put in as  the table
// 'addRowName' is 'true' if to add an initial column with the rowname
// 'rebuild' is 'true' if to rebuild the  table every time called
//     otherwise the table is built once and reused in later calles
// The table built has IDs and classes added to every cell so
// one can put data into the cell and format the cells and columns.
// If a table for 'SECT' is built and the data has 'row' and 'col'
// entries:
//   "rowA": { "colA": "dataAA", "colB":"dataAB", ...},
//   "rowB": { "colA": "dataBA", "colB":"dataBB", ...}, ...
// then the table built has"
// |table id='SECT-table' class='SECT-table-class'|
// |tr| |th class="SECT-rowName-header"| |th class="SECT-COLX-header"| ... |/tr|
// |tr| |td id="SECT-rowName-ROWX" class="SECT--class"| |/td|
//      |td id="SECT-ROWX-COLX" class="SECT-COLX-class"| |/td|
//      ...
// |/tr|
// ... 
// |/table|
//
// Function takes three additional arguments:
//     addRowName: add a header row of the column names (default=true)
//     addDisplayCol: add a last column named display (default=false)
//     columns: an array of column names to display. If empty, build
//       the column names from the data
function BuildTableForData(sectID: string, tableID: string, data: TableData,
            pAddRowName?: boolean, pAddDisplayCol?: boolean, pColumns?: Array<string>) : HTMLElement {
    var addRowName = pAddRowName ?? true;
    var addDisplayCol = pAddDisplayCol ?? false;

    var columns = pColumns ?? new Array<string>();
    if (addDisplayCol) {
        columns.push("Display");
    }

    // create a table with td's with id's for cell addressing
    var tbl = aTable(tableID, MakeID(sectID + '-table-class'));
    var headerRow = aTableRow(undefined, MakeID(sectID + '-headerRow'));
    if (addRowName) {
        var headerCell = aTableHeaderCell(MakeID(sectID + '-rowName-header'));
        headerRow.appendChild(headerCell);
    }
    if (columns.length != 0) {
        for (let col in columns) {
            var headerCell = aTableHeaderCell(MakeID(sectID + '-' + columns[col] + '-header'));
            headerCell.appendChild(document.createTextNode(columns[col]));
            headerRow.appendChild(headerCell);
        }
    }
    tbl.appendChild(headerRow);
    for (let row in data) {
        var tableRow = aTableRow();
        if (addRowName) {
            var rowCell = aTableData(MakeID(sectID + '--class'), MakeID(sectID + '-rowName-' + row));
            rowCell.appendChild(document.createTextNode('.'));
            tableRow.appendChild(rowCell);
        }
        for (let col in columns) {
            var cell = aTableData(MakeID(sectID + '-' + col + '-class'), MakeID(sectID + '-' + row + '-' + col));
            cell.appendChild(document.createTextNode('.'));
            tableRow.appendChild(cell);
        }
        tbl.appendChild(tableRow);
    }
    return tbl;
}

// ===========================================
// table in the specified section.
// Clear whatever is there and refill it with the data.
function BuildBasicTable(sect:string, data:any, paddRowName?: boolean, prebuild?: boolean, paddDisplayCol?: boolean) : void {
    var sectID = MakeID(sect)
    var tableID = MakeID(sectID + "-table")
    var addRowName = paddRowName ?? true;
    var rebuild = prebuild ?? false;
    var addDisplayCol = paddDisplayCol ?? false;

    var specifyColumns = new Array();
    if (arguments.length > 5) specifyColumns = arguments[5];

    if (document.getElementById(tableID) == null || rebuild) {
        // table does not exist. Build same
        var sectElem = document.querySelector(sect);
        if (sectElem != null) {
            sectElem.innerHTML = "";
            sectElem.appendChild(BuildTableForData(sectID, tableID, data, addRowName, addDisplayCol, specifyColumns));
        }
    }
    // Fill its cells with the text data
    for (let row in data) {
        var rowName = document.getElementById(MakeID(sectID + '-rowName-' + row));
        if (rowName != null) rowName.textContent = row;
        for (let col in data[row]) {
            var cellID = MakeID(sectID + '-' + row + '-' + col);
            if (document.getElementById(cellID) != null) {
                document.getElementById(cellID)!.textContent = data[row][col];
            }
        }
    }
}
// ===========================================
function aTable(className?:string, idName?:string) : HTMLElement {
    return anElement("table", className, idName);
}
function aTableRow(className?:string, idName?:string) : HTMLElement {
    return anElement("tr", className, idName);
}
function aTableHeaderCell(className?:string, idName?:string) : HTMLElement {
    return anElement("th", className, idName);
}
function aTableData(className?:string, idName?:string) : HTMLElement {
    return anElement("td", className, idName);
}
function anElement(tagName:string, className?:string, idName?:string) : HTMLElement {
    var ret = document.createElement(tagName);
    if (className != undefined) ret.setAttribute("class", className);
    if (idName != undefined) ret.setAttribute("id", idName);
    return ret;
}

// clean up ID so there are no dots
function MakeID(inID:string):string {
    return inID.replace(/\./g, '-');
}

// ===========================================
// Log debug message to the DEBUGG div if it exists
// If classname is specified, add that class to the new div
function LogDebug(msg: string, classname?: string) : void {
    const debugg = document.getElementById("DEBUGG");
    if (debugg != undefined) {
        const newline = document.createElement("div");
        newline.appendChild(document.createTextNode(msg));
        if (classname != undefined) {
            newline.setAttribute("class", classname);
        }
        debugg.appendChild(newline);
        if (debugg.childElementCount > 20) {
            debugg.removeChild(debugg.firstChild as ChildNode);
        }
    }
}

// ===========================================
// Class for keeping and displaying trending data.
// A new instance of the class is created and instance.AddPoint(pnt)
// is called to add data points. A sequence of up to
// instance.maxDataPoints (default 100) are collected with old
// being thrown away.
// The display is put into the page by calling instance.InsertDisplay(id)
// where id is an HTML element id to put the code. After placing the
// code, the display will be automatically updated.
// Formatting is done via formatting params passed at creation.
// Set format before inserting the display html.
// Look at the default example below for the options.
// Uses 'sparklines' so you must include that script library.
class TrendData {
    maxDataPoints = 100;
    dataPoints = new Array<number>();
    formatParms = {
        type: 'line', // line (default), bar, tristate, discrete, bullet, pie or box
        width: 'auto',      // 'auto' or any css width spec
        height: 'auto',     // 'auto' or any valid css height spec
        lineColor: 'black', // Used by line and discrete charts
        // chartRangeMin: '0', // min value for range, default to min value
        // chardRangeMax: '0', // max value for range, default to max value
        // composite: 'true',  // true to overwrite existing chart (chart on chart)
        fillColor: 'false'  // Set to false to disable fill.
    };

    constructor(numberOfPoints: number) {
        this.maxDataPoints = numberOfPoints;
        for (var ii=0; ii<this.maxDataPoints; ii++) this.dataPoints[ii] = 0;
    }
    AddPoint(pnt: number) {
        for (var ii=this.maxDataPoints-1; ii>0; ii--) {
            this.dataPoints[ii] = this.dataPoints[ii-1];
        }
        this.dataPoints[0] = pnt;
    }
    UpdateDisplay(id: string) {
        var spark = document.getElementById(id);
        // spark.sparkline_display_visible();
        // spark.sparkline(this.dataPoints, this.formatParms);
    }
    Format(format: any) {
        this.formatParms = format;
    }

}
// ===========================================




