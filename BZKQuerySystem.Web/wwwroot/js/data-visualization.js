// 数据可视化管理器
class DataVisualizationManager {
    constructor() {
        this.charts = new Map(); // 存储图表实例
        this.chartConfigs = new Map(); // 存储图表配置
        this.defaultColors = [
            '#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', 
            '#9966FF', '#FF9F40', '#FF6384', '#C9CBCF'
        ];
        
        this.init();
    }

    // 初始化
    init() {
        this.setupEventHandlers();
        console.log('Data visualization manager initialized');
    }

    // 设置事件处理器
    setupEventHandlers() {
        // 监听查询完成事件
        document.addEventListener('signalr-queryCompleted', (event) => {
            const result = event.detail;
            if (result.isSuccess && result.recordCount > 0) {
                this.analyzeDataForVisualization(result);
            }
        });

        // 图表类型选择变化
        document.addEventListener('change', (event) => {
            if (event.target.classList.contains('chart-type-selector')) {
                this.handleChartTypeChange(event);
            }
        });

        // 图表配置变化
        document.addEventListener('change', (event) => {
            if (event.target.classList.contains('chart-config-input')) {
                this.handleConfigChange(event);
            }
        });
    }

    // 分析数据并提供可视化选项
    analyzeDataForVisualization(queryResult) {
        // 从结果表格中分析数据结构
        const resultTable = document.querySelector('#results-table');
        if (resultTable) {
            const data = this.extractTableData(resultTable);
            if (data.length > 0) {
                this.showVisualizationOptions(data);
            }
        }
    }

    // 从表格提取数据
    extractTableData(table) {
        const data = [];
        const headers = [];
        
        // 获取表头
        const headerRow = table.querySelector('thead tr');
        if (headerRow) {
            headerRow.querySelectorAll('th').forEach(th => {
                headers.push(th.textContent.trim());
            });
        }

        // 获取数据行
        const bodyRows = table.querySelectorAll('tbody tr');
        bodyRows.forEach(row => {
            const rowData = {};
            row.querySelectorAll('td').forEach((td, index) => {
                if (index < headers.length) {
                    rowData[headers[index]] = td.textContent.trim();
                }
            });
            data.push(rowData);
        });

        return { headers, data };
    }

    // 显示可视化选项
    showVisualizationOptions(tableData) {
        const container = this.getOrCreateVisualizationContainer();
        const { headers, data } = tableData;
        
        // 分析数据类型
        const analysis = this.analyzeDataTypes(headers, data);
        
        // 生成可视化建议
        const suggestions = this.generateVisualizationSuggestions(analysis);
        
        // 创建可视化选项UI
        const optionsHtml = this.createVisualizationOptionsHtml(suggestions, headers);
        container.innerHTML = optionsHtml;
        
        // 显示容器
        container.style.display = 'block';
    }

    // 分析数据类型
    analyzeDataTypes(headers, data) {
        const analysis = {
            numericColumns: [],
            textColumns: [],
            dateColumns: [],
            totalRows: data.length
        };

        headers.forEach(header => {
            const values = data.map(row => row[header]).filter(v => v && v.trim());
            
            if (values.length === 0) return;
            
            // 检测是否为数值列
            const numericValues = values.filter(v => !isNaN(parseFloat(v)) && isFinite(parseFloat(v)));
            if (numericValues.length > values.length * 0.8) {
                analysis.numericColumns.push({
                    name: header,
                    values: numericValues.map(v => parseFloat(v)),
                    min: Math.min(...numericValues.map(v => parseFloat(v))),
                    max: Math.max(...numericValues.map(v => parseFloat(v)))
                });
                return;
            }
            
            // 检测是否为日期列
            const dateValues = values.filter(v => !isNaN(Date.parse(v)));
            if (dateValues.length > values.length * 0.8) {
                analysis.dateColumns.push({
                    name: header,
                    values: dateValues.map(v => new Date(v))
                });
                return;
            }
            
            // 默认为文本列
            analysis.textColumns.push({
                name: header,
                values: values,
                uniqueCount: [...new Set(values)].length
            });
        });

        return analysis;
    }

    // 生成可视化建议
    generateVisualizationSuggestions(analysis) {
        const suggestions = [];
        
        // 柱状图建议
        if (analysis.textColumns.length > 0 && analysis.numericColumns.length > 0) {
            suggestions.push({
                type: 'bar',
                title: 'Bar Chart - Category Analysis',
                description: 'Display value distribution across categories',
                priority: 1,
                config: {
                    categoryColumn: analysis.textColumns[0].name,
                    valueColumn: analysis.numericColumns[0].name
                }
            });
        }

        // 饼图建议
        if (analysis.textColumns.length > 0) {
            const textCol = analysis.textColumns[0];
            if (textCol.uniqueCount <= 10 && textCol.uniqueCount > 1) {
                suggestions.push({
                    type: 'pie',
                    title: 'Pie Chart - Proportion Display',
                    description: 'Show proportion distribution',
                    priority: 2,
                    config: {
                        categoryColumn: textCol.name
                    }
                });
            }
        }

        // 折线图建议
        if (analysis.dateColumns.length > 0 && analysis.numericColumns.length > 0) {
            suggestions.push({
                type: 'line',
                title: 'Line Chart - Trend Analysis',
                description: 'Show trend over time',
                priority: 1,
                config: {
                    dateColumn: analysis.dateColumns[0].name,
                    valueColumn: analysis.numericColumns[0].name
                }
            });
        }

        // 散点图建议
        if (analysis.numericColumns.length >= 2) {
            suggestions.push({
                type: 'scatter',
                title: 'Scatter Plot - Correlation Analysis',
                description: 'Analyze relationship between variables',
                priority: 3,
                config: {
                    xColumn: analysis.numericColumns[0].name,
                    yColumn: analysis.numericColumns[1].name
                }
            });
        }

        return suggestions.sort((a, b) => a.priority - b.priority);
    }

    // 创建可视化选项UI
    createVisualizationOptionsHtml(suggestions, headers) {
        if (suggestions.length === 0) {
            return `
                <div class="alert alert-info">
                    <i class="fas fa-info-circle"></i>
                    No visualization options available for current data.
                </div>
            `;
        }

        let html = `
            <div class="visualization-header">
                <h5><i class="fas fa-chart-bar"></i> Data Visualization</h5>
                <button class="btn btn-sm btn-outline-secondary" onclick="dataVisualization.toggleVisualizationPanel()">
                    <i class="fas fa-eye-slash"></i> Hide Panel
                </button>
            </div>
            <div class="visualization-suggestions">
                <h6>Recommended Chart Types:</h6>
                <div class="row">
        `;

        suggestions.forEach((suggestion, index) => {
            html += `
                <div class="col-md-6 col-lg-4 mb-3">
                    <div class="card chart-suggestion ${index === 0 ? 'selected' : ''}" 
                         data-chart-type="${suggestion.type}" 
                         data-config='${JSON.stringify(suggestion.config)}'>
                        <div class="card-body">
                            <h6 class="card-title">
                                <i class="fas ${this.getChartIcon(suggestion.type)}"></i>
                                ${suggestion.title}
                            </h6>
                            <p class="card-text">${suggestion.description}</p>
                            <button class="btn btn-primary btn-sm" onclick="dataVisualization.createChart('${suggestion.type}', ${JSON.stringify(suggestion.config).replace(/"/g, '&quot;')})">
                                Create Chart
                            </button>
                        </div>
                    </div>
                </div>
            `;
        });

        html += `
                </div>
            </div>
            <div class="visualization-custom">
                <h6>Custom Visualization:</h6>
                <div class="row">
                    <div class="col-md-4">
                        <label class="form-label">Chart Type</label>
                        <select class="form-select chart-type-selector">
                            <option value="bar">Bar Chart</option>
                            <option value="line">Line Chart</option>
                            <option value="pie">Pie Chart</option>
                            <option value="doughnut">Doughnut Chart</option>
                            <option value="scatter">Scatter Plot</option>
                            <option value="radar">Radar Chart</option>
                        </select>
                    </div>
                    <div class="col-md-4">
                        <label class="form-label">X-Axis Field</label>
                        <select class="form-select chart-config-input" id="chart-x-field">
                            ${headers.map(h => `<option value="${h}">${h}</option>`).join('')}
                        </select>
                    </div>
                    <div class="col-md-4">
                        <label class="form-label">Y-Axis Field</label>
                        <select class="form-select chart-config-input" id="chart-y-field">
                            ${headers.map(h => `<option value="${h}">${h}</option>`).join('')}
                        </select>
                    </div>
                </div>
                <div class="mt-3">
                    <button class="btn btn-success" onclick="dataVisualization.createCustomChart()">
                        <i class="fas fa-chart-line"></i> Create Custom Chart
                    </button>
                    <button class="btn btn-info" onclick="dataVisualization.showDashboard()">
                        <i class="fas fa-tachometer-alt"></i> View Dashboard
                    </button>
                </div>
            </div>
            <div id="chart-container" class="chart-container mt-4" style="display: none;">
                <canvas id="data-chart"></canvas>
            </div>
        `;

        return html;
    }

    // 获取图表图标
    getChartIcon(chartType) {
        const icons = {
            'bar': 'fa-chart-bar',
            'line': 'fa-chart-line',
            'pie': 'fa-chart-pie',
            'doughnut': 'fa-chart-pie',
            'scatter': 'fa-braille',
            'radar': 'fa-chart-area'
        };
        return icons[chartType] || 'fa-chart-bar';
    }

    // 创建可视化
    async createChart(type, config) {
        try {
            const tableData = this.extractTableData(document.querySelector('#results-table'));
            const chartData = this.prepareChartData(type, config, tableData);
            
            if (!chartData) {
                this.showError('Failed to create visualization. Please check data.');
                return;
            }

            const chartContainer = document.getElementById('chart-container');
            const canvas = document.getElementById('data-chart');
            
            // 销毁现有图表实例
            if (this.charts.has('main-chart')) {
                this.charts.get('main-chart').destroy();
            }

            // 创建新图表实例
            const ctx = canvas.getContext('2d');
            const chart = new Chart(ctx, {
                type: type,
                data: chartData,
                options: this.getChartOptions(type)
            });

            this.charts.set('main-chart', chart);
            chartContainer.style.display = 'block';

            // 添加图表控制按钮
            this.addChartControls(chartContainer);

            console.log(`Successfully created ${type} chart`);
        } catch (error) {
            console.error('Failed to create visualization:', error);
            this.showError('Failed to create visualization: ' + error.message);
        }
    }

    // 准备图表数据
    prepareChartData(type, config, tableData) {
        const { headers, data } = tableData;
        
        switch (type) {
            case 'bar':
            case 'line':
                return this.prepareBarLineData(config, data);
            case 'pie':
            case 'doughnut':
                return this.preparePieData(config, data);
            case 'scatter':
                return this.prepareScatterData(config, data);
            case 'radar':
                return this.prepareRadarData(config, data);
            default:
                return null;
        }
    }

    // 准备柱状图或折线图数据
    prepareBarLineData(config, data) {
        const categoryField = config.categoryColumn || config.xColumn;
        const valueField = config.valueColumn || config.yColumn;
        
        if (!categoryField || !valueField) return null;

        const groupedData = {};
        data.forEach(row => {
            const category = row[categoryField];
            const value = parseFloat(row[valueField]);
            
            if (!isNaN(value)) {
                if (!groupedData[category]) {
                    groupedData[category] = [];
                }
                groupedData[category].push(value);
            }
        });

        const labels = Object.keys(groupedData);
        const values = labels.map(label => {
            const categoryValues = groupedData[label];
            return categoryValues.reduce((sum, val) => sum + val, 0) / categoryValues.length;
        });

        return {
            labels: labels,
            datasets: [{
                label: valueField,
                data: values,
                backgroundColor: this.defaultColors.slice(0, labels.length),
                borderColor: this.defaultColors.slice(0, labels.length),
                borderWidth: 1
            }]
        };
    }

    // 准备饼图或环形图数据
    preparePieData(config, data) {
        const categoryField = config.categoryColumn;
        if (!categoryField) return null;

        const counts = {};
        data.forEach(row => {
            const category = row[categoryField];
            counts[category] = (counts[category] || 0) + 1;
        });

        const labels = Object.keys(counts);
        const values = Object.values(counts);

        return {
            labels: labels,
            datasets: [{
                data: values,
                backgroundColor: this.defaultColors.slice(0, labels.length),
                borderWidth: 1
            }]
        };
    }

    // 准备散点图数据
    prepareScatterData(config, data) {
        const xField = config.xColumn;
        const yField = config.yColumn;
        
        if (!xField || !yField) return null;

        const points = data.map(row => {
            const x = parseFloat(row[xField]);
            const y = parseFloat(row[yField]);
            
            if (!isNaN(x) && !isNaN(y)) {
                return { x, y };
            }
            return null;
        }).filter(point => point !== null);

        return {
            datasets: [{
                label: `${yField} vs ${xField}`,
                data: points,
                backgroundColor: this.defaultColors[0],
                borderColor: this.defaultColors[0]
            }]
        };
    }

    // 准备雷达图数据
    prepareRadarData(config, data) {
        const dimensions = config.dimensions;
        if (!dimensions || dimensions.length === 0) return null;

        // 计算平均值
        const averages = dimensions.map(dim => {
            const values = data.map(row => parseFloat(row[dim])).filter(v => !isNaN(v));
            return values.length > 0 ? values.reduce((sum, val) => sum + val, 0) / values.length : 0;
        });

        return {
            labels: dimensions,
            datasets: [{
                label: 'Average Values',
                data: averages,
                backgroundColor: 'rgba(54, 162, 235, 0.2)',
                borderColor: 'rgba(54, 162, 235, 1)',
                borderWidth: 2
            }]
        };
    }

    // 获取图表选项
    getChartOptions(type) {
        const commonOptions = {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'top'
                },
                title: {
                    display: true,
                    text: 'Data Visualization'
                }
            }
        };

        return commonOptions;
    }

    // 其他方法保持简化版本
    addChartControls(container) {
        // 简化版本，不添加复杂的控制按钮
    }

    createCustomChart() {
        const chartType = document.querySelector('.chart-type-selector').value;
        const xField = document.getElementById('chart-x-field').value;
        const yField = document.getElementById('chart-y-field').value;

        const config = {
            xColumn: xField,
            yColumn: yField,
            categoryColumn: xField,
            valueColumn: yField
        };

        this.createChart(chartType, config);
    }

    showDashboard() {
        console.log('Dashboard feature simplified');
    }

    toggleVisualizationPanel() {
        const container = document.getElementById('visualization-container');
        if (container) {
            container.style.display = container.style.display === 'none' ? 'block' : 'none';
        }
    }

    getOrCreateVisualizationContainer() {
        let container = document.getElementById('visualization-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'visualization-container';
            container.className = 'visualization-container mt-4 p-4 border rounded';
            container.style.display = 'none';
            
            const resultsSection = document.querySelector('#results-section') || document.querySelector('.results-container');
            if (resultsSection) {
                resultsSection.appendChild(container);
            } else {
                document.body.appendChild(container);
            }
        }
        return container;
    }

    showError(message) {
        console.error(message);
        if (window.queryNotificationClient) {
            window.queryNotificationClient.showToastNotification({
                title: 'Data Visualization',
                message: message,
                type: 'Error'
            });
        } else {
            alert('Error: ' + message);
        }
    }

    handleChartTypeChange(event) {
        console.log('Chart type changed:', event.target.value);
    }

    handleConfigChange(event) {
        console.log('Chart config changed:', event.target.id, event.target.value);
    }

    destroyAllCharts() {
        this.charts.forEach(chart => chart.destroy());
        this.charts.clear();
    }
}

// 全局数据可视化对象
let dataVisualization = null;

// 初始化数据可视化管理器
document.addEventListener('DOMContentLoaded', function() {
    if (typeof Chart !== 'undefined') {
        dataVisualization = new DataVisualizationManager();
        window.dataVisualization = dataVisualization;
        console.log('Data visualization manager initialized successfully');
    } else {
        console.error('Chart.js library not loaded, cannot initialize data visualization manager');
    }
}); 