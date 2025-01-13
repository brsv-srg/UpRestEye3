//window.initializeTableResizer = function (tableId) {
//    const table = document.getElementById(tableId);
//    if (!table) {
//        console.error(`Table with ID ${tableId} not found`); // Add error handling if table is not found
//        return;
//    }
//    const cols = table.querySelectorAll('th');
//    cols.forEach(col => {
//        const resizer = document.createElement('div');
//        resizer.classList.add('resizer');
//        col.appendChild(resizer);
//        resizer.addEventListener('mousedown', initResize);
//    });

//    function initResize(e) {
//        const col = e.target.parentElement;
//        const startX = e.pageX;
//        const startWidth = col.offsetWidth;

//        function doResize(e) {
//            col.style.width = startWidth + (e.pageX - startX) + 'px';
//        }

//        function stopResize() {
//            document.removeEventListener('mousemove', doResize);
//            document.removeEventListener('mouseup', stopResize);
//        }

//        document.addEventListener('mousemove', doResize);
//        document.addEventListener('mouseup', stopResize);
//    }
//};

if (typeof isResizing === 'undefined')
    var isResizing = false; 

if (typeof currentTh === 'undefined')
    var currentTh = null;

if (typeof startX === 'undefined')
    var startX;

if (typeof startWidth === 'undefined')
    var startWidth;

if (typeof _resizableTable === 'undefined')
    var _resizableTable;


window.initResizableTable = function (tableId) {
    _resizableTable = document.getElementById(tableId);
    if (!_resizableTable) {
        console.error(`Table with ID ${tableId} not found`); // Add error handling if table is not found
        return;
    }
    //document.addEventListener('mousedown', initResize);
    document.addEventListener('mousemove', handleMouseMove);
	document.addEventListener('mouseup', handleMouseUp);
};

//function initResize(e) {
//    document.addEventListener('mousemove', handleMouseMove);
//    document.addEventListener('mouseup', handleMouseUp);
//};

window.startResize = function (columnIndex) {
    if (!_resizableTable)
        return;

    const th = _resizableTable.querySelectorAll('th')[columnIndex];
    
    isResizing = true;
    currentTh = th;
    startX = event.pageX;
    startWidth = currentTh.offsetWidth;
    //document.addEventListener('mousemove', handleMouseMove);
    //document.addEventListener('mouseup', handleMouseUp);

};

function handleMouseMove(e) {
    if (!isResizing) return;

    const width = startWidth + (e.pageX - startX);
    if (width > 50) { // Минимальная ширина колонки
        currentTh.style.width = `${width}px`;
    }
}

function handleMouseUp() {
    isResizing = false;
    currentTh = null;
    document.removeEventListener('mousemove', handleMouseMove);
    document.removeEventListener('mouseup', handleMouseUp);
}
