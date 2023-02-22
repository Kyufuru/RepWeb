var app = new Vue({
    el: "#vue",
    data: {
        bom: [],
        mat: [],
        mats: [],
        options: [],
        confs: [],
        sel: [],
        user: "游客",
        curTab: "1",
        loadNum: 20,
        curSelId: -1,
        curConf: {},
        setting: {
            wH: window.innerHeight,
            tload: false,
            cload: false,
            del: false,
            add: false,
            load: false,
            sav: false,
            edit: false,
            matAll: false,
            onlyRead: true
        },
        form: {
            类别: "",
            名称: "",
            图纸下发实际日期: "",
            材料到货实际日期: "",
        },
        conf: {
            ID: 0,
            标题: "",
            配置名称: "",
            工程号: "",
            公司名称: "",
            负责人: "",
            项目开始日期: "",
            调整滚动日期: 0,
            修改日期: "",
        },
        rules: {
            类别: [
                { required: true, message: '请输入类别', trigger: 'change' },
            ],
            名称: [
                { required: true, message: '请输入名称', trigger: 'change' },
            ],
        },
        err: {
            "-1": "错误：未选择数据",
            "-2": "错误：编辑多条数据",
            "-3": "错误：未加载数据",
            "-4": "错误：配置名称已存在",
            "-5": "错误：无法解析文件"
        }
    },
    computed: {
        // 内容高度
        mainHeight() {
            return { "height": `${this.setting.wH - 122}px` }
        },
        // 加载数量
        partOptions() {
            return this.options.slice(0, this.loadNum)
        },
        progType() {
            return new Set(this.mat.map(v => v.类别))
        },
        progName() {
            return new Set(this.mat.map(v => v.名称))
        },
        startDate() {
            let d = this.conf.项目开始日期 ? dayjs(this.conf.项目开始日期) : dayjs()
            return this.conf.调整滚动日期 ? d.add(this.conf.调整滚动日期, "d") : d
        },
        endDate() {
            return this.startDate.add(5, "M")
        },
        gantt() {
            let gantt = []

            this.mat.forEach(v => {
                gantt.push({
                    name: `${v.名称} 计划`,
                    style: {
                        "background-color": "ghostwhite"
                    },
                    gtArray: [{
                        start: v.图纸下发计划日期,
                        end: v.材料到货计划日期
                    }]
                })
                gantt.push({
                    name: `${v.名称} 实际`,
                    style: {
                        "background-color": "white"
                    },
                    gtArray: [{
                        start: v.图纸下发实际日期,
                        end: (!v.材料到货实际日期 && v.图纸下发实际日期) ? dayjs() : v.材料到货实际日期,
                        deadLine: v.材料到货计划日期
                    }]
                })
            })
            return gantt
        }
    },
    methods: {
        // 显示消息
        msg(msg, type) {
            const offset = 10
            const duration = 2000
            this.$message[type]({
                message: msg,
                offset: offset,
                duration: duration
            })
        },
        // 显示数据文件加载状态
        dataLoading() {
            this.setting.tload = true
        },
        // 加载数据文件并计算数据
        dataLoaded(res, f, flist) {
            this.$set(this, "conf", res.conf)
            this.$set(this, "mat", res.mat)
            this.$set(this, "bom", res.bom)

            axios.get(
                `/api/Rep/GetMats?pno=${this.conf.工程号}`
            ).then(res => this.mats = res.data)

            this.mat.forEach(x => this.calc(x))
            this.setting.tload = false
        },
        // 数据文件加载错误
        dataError() {
            this.setting.tload = false
            this.msg(this.err["-5"], "error")
        },
        // 计算数据
        calc(row) {
            let today = dayjs().format("YYYY-MM-DD")
            row.图纸下发计划日期 = this.dateFormat(row.图纸下发计划日期)
            row.图纸下发实际日期 = this.dateFormat(row.图纸下发实际日期)
            row.材料到货计划日期 = this.dateFormat(row.材料到货计划日期)
            row.材料到货实际日期 = this.dateFormat(row.材料到货实际日期)
            row.计划天数 = this.dateDiff(row.材料到货计划日期, row.图纸下发计划日期)
            row.实际天数 = this.dateDiff(row.材料到货实际日期, row.图纸下发实际日期)
            row.研发超期天数 = this.dateDiff(row.图纸下发实际日期 ? row.图纸下发实际日期 : today, row.图纸下发计划日期)
            row.到货超期天数 = this.dateDiff(row.材料到货实际日期 ? row.材料到货实际日期 : today, row.材料到货计划日期)
            row.采购订单进度 = row.订单完成数量 / row.单据数量 * 100
            row.采购到货进度 = row.到货完成数量 / row.单据数量 * 100
            row.状态 = this.dateSta(row)
            row.一周预警 = row.到货超期天数 >= -7 && row.到货超期天数 <= 0
        },
        // 总进度
        total() {
            if (!this.mat || this.mat.length == 0) return 0

            return this.mat.map(v => v.采购到货进度).reduce(
                (pre, cur) => {
                    if (isNaN(pre)) pre = 0
                    if (isNaN(cur)) cur = 0
                    return pre + cur
                }) / this.mat.length
        },
        // 日期格式
        dateFormat(str) {
            let date = dayjs(str)
            if (!str || !date.isValid()) return str
            else return date.format("YYYY-MM-DD")
        },
        // 日期差异
        dateDiff(d1, d2) {
            let diff = dayjs(d1).diff(dayjs(d2), "d")
            if (d1 && d2 && !isNaN(diff)) return diff
        },
        // 延期状态
        dateSta(row) {
            switch (Math.sign(row.到货超期天数)) {
                case -1:
                    if (row.材料到货实际日期) return ["success", "提前完成"]
                    break
                case 0:
                    if (row.材料到货实际日期) return ["warning", "按期完成"]
                    break
                case 1:
                    if (row.材料到货实际日期) return ["danger", "超期完成"]
                    else return ["danger", "超期"]
            }
            if (row.采购订单进度 || row.采购到货进度) return ["", "进行中"]
            else return ["info", "未开始"]
        },
        // BOM显示
        bomIsExist(b) {
            return b && b.length > 1
        },
        // BOM提示条显示
        bomToolTip(b) {
            return b ? b.join('<br/>') : ''
        },
        // 物料检索
        matFind(q) {
            if (q != "") {
                this.loadNum = 20
                this.options = this.mats.filter(
                    x => !q.split(" ").map(
                            e => x.物料名称.indexOf(e.toUpperCase()) > -1
                        ).includes(false)
                )
            }
        },
        // 物料全选
        matAllSel(row) {
            if (this.setting.matAll)
                row.产品名称 = this.options.map(v => v.物料名称)
            else row.产品名称 = []
            this.bomMatch(row)
        },
        bomIsAllSel(b) {
            if (this.options.length > 0 && this.options.length == b.length)
                this.setting.matAll = true
            else this.setting.matAll = false
        },
        // 物料更多
        matLoadMore() {
            this.loadNum += 20
        },
        // BOM匹配
        bomMatch(row) {
            let b = this.bom.filter(b => row.产品名称.includes(b.产品名称))
            if (b.length > 0) {
                b = b.reduce((pre, cur) => {
                    if (this.dateDiff(cur.图纸下发实际日期, pre.图纸下发实际日期) > 0)
                        cur.图纸下发实际日期 = pre.图纸下发实际日期
                    if (this.dateDiff(cur.材料到货实际日期, pre.材料到货实际日期) > 0)
                        cur.材料到货实际日期 = pre.材料到货实际日期
                    cur.订单完成数量 += pre.订单完成数量
                    cur.到货完成数量 += pre.到货完成数量
                    cur.单据数量 += pre.单据数量
                    return cur
                })
            }
            row.图纸下发实际日期 = b.图纸下发实际日期
            row.材料到货实际日期 = b.材料到货实际日期
            row.订单完成数量 = b.订单完成数量
            row.到货完成数量 = b.到货完成数量
            row.单据数量 = b.单据数量
            this.calc(row)
        },

        format(p) {
            return p.toFixed(2) + "%"
        },
        
        getConfs() {
            this.setting.cload = true
            let f = "YYYY-MM-DD HH:mm:ss"
            axios.get("/api/Rep/GetConfs").then(
                res => {
                    this.$set(this, "confs", res.data)
                    this.confs.forEach(x => x.修改日期 = dayjs(x.修改日期).format(f))
                    this.setting.cload = false
                }
            )
        },
        // 计算
        barStyle(data) {
            let color
            let end = dayjs(data.gtArray[0].end)
            let ddl = dayjs(data.gtArray[0].deadLine)
            let type = data.name
            if (type.endsWith("计划")) color = "#555"
            else if (end.isBefore(ddl, "D")) color = "#2C94FF"
            else if (end.isAfter(ddl, "D")) color = "#FA5757"
            else color = "#19a375"

            return { "background-color": color }
        },
        // 选择
        rowClick(row) { this.$refs.tdata.toggleRowSelection(row) },
        select(sel) { this.sel = sel },
        selConf(conf) { this.curConf = conf },
        // 处理
        validate(op) {
            let e = 0;
            let num = this.sel.length

            if (op != "load" && this.mat.length == 0) e = -3
            else switch (op) {
                case "edit":
                    if (num == 0) e = -1
                    else if (num > 1) e = -2
                    break
                case "del":
                    if (num == 0) e = -1
                    break
                case "sav":
                    break
                case "pnt":
                    if (num == 0) e = -1
                    break
            }
            if (e < 0) this.msg(this.err[`${e}`], "error")
            return e
        },
        showDial(op) {
            if (this.validate(op) >= 0) {
                this.clear()
                this.setting[op] = true
            }
        },
        clear() {
            Object.keys(this.form).forEach(k => this.form[k] = "")
        },
        delConf(scope) {
            axios.post(
                "/api/Rep/DelConf", scope.row
            ).then(() => this.confs.splice(scope.$index,1))
        },
        add() {
            this.$refs["newItem"].validate((valid) => {
                if (!valid) return false
                let id = 0
                if (this.sel.length > 0)
                    id = this.mat.indexOf(this.sel[0]) + 1
                this.calc(this.form)
                this.mat.splice(id, 0, JSON.parse(JSON.stringify(this.form)))
                this.setting.add = false
                this.$refs["newItem"].clearValidate()
                this.$refs["newItem"].resetFields()
                this.msg("操作成功", "success")
            })
        },
        save() {
            this.setting.sav = true
            let matSav = []
            this.mat.forEach(x => {
                if (x.产品名称.length == 0) {
                    matSav.push(JSON.parse(JSON.stringify(x)))
                    matSav.at(-1).产品名称 = null
                }
                else
                    x.产品名称.forEach(n => {
                        matSav.push(JSON.parse(JSON.stringify(x)))
                        matSav.at(-1).产品名称 = n
                    })
            })
            console.log(matSav)
            axios.post(
                "/api/Rep/Save",
                {
                    conf: this.conf,
                    mat: matSav
                }
            ).then(() => {
                this.setting.load = this.setting.sav = false
                this.msg("操作成功", "success")
            })
        },
        print() {
            let pnt = {
                conf: {
                    标题: this.conf.标题,
                    工程号: this.conf.工程号,
                    公司名称: this.conf.公司名称,
                    负责人: this.conf.负责人
                },
                data: JSON.parse(JSON.stringify(this.sel))
            }
            console.log(pnt)
            axios.post("/api/Rep/Print", pnt).then(
                () => window.open("http://192.168.0.220:18301/digiwin/kanban/view/report?"
                    + "viewlet=%25E6%2596%25B0%25E8%2583%25BD%25E9%2587%2587%25E8%25B4%25AD%25E8"
                    + "%25BF%259B%25E5%25BA%25A6%25E8%25A1%25A8%25E6%2589%2593%25E5%258D%25B0.cpt"
                    + "&ref_t=design&ref_c=7a4d1092-bda9-428a-bc2f-cbba276fd906", "_blank")
            )
        },
        tblExecute(op) {
            if (this.validate(op) < 0) return
            switch (op) {
                case "del":
                    this.sel.forEach(v => this.mat.splice(this.mat.indexOf(v), 1))
                    break
                case "add":
                    this.add()
                    break
                case "load":
                    this.setting.tload = true
                    axios.post(
                        "/api/Rep/Load", this.curConf
                    ).then(res => {
                        this.dataLoaded(res.data)
                        this.setting.load = this.setting.sav = false
                    })
                    break
                case "sav":
                    if (this.confs.map(v => v.配置名称).indexOf(this.conf.配置名称) != -1)
                        this.$confirm('存在同名配置，是否覆盖?', '提示', {
                            confirmButtonText: '确定',
                            cancelButtonText: '取消',
                            type: 'warning'
                        }).then(() => this.save())
                    else this.save()
                    break
                case "pnt":
                    this.print()
                    break
                case "exp":
                    let data = JSON.parse(JSON.stringify(this.mat))
                    data.forEach(r => {
                        r.产品名称 = `${r.产品名称}`
                        r.状态 = r.状态[1]
                        r.一周预警 = r.一周预警? "!": ""
                    })
                    let wb = XLSX.utils.book_new()
                    let ws = XLSX.utils.json_to_sheet(data);
                    XLSX.utils.book_append_sheet(wb, ws);
                    XLSX.writeFile(wb, `${this.conf.配置名称}.xlsx`);
                    break
            }
        },
    },
    mounted() {
        this.user = new URLSearchParams(window.location.href).get("user");
        if (!this.user) this.user = "游客";
        if (this.user.toLowerCase().startsWith("pmc")) this.setting.onlyRead = false;
        axios.post(
            "/api/Entry/AddStat",
            {
                用户: this.user,
                渠道: "加载",
                报表名称: "新能采购进度表",
                本地时间: dayjs().format(),
            }
        )
        window.onresize = () => this.setting.wH = window.innerHeight
    }
})