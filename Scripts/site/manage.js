var app = new Vue({
    el: "#vue",
    data: {
        dialTitle: "",
        curTab: "user",
        curPage: 1,
        query: "",
        tblLen: 0,
        tables: null,
        form: {},
        sel: [],
        setting: {
            winH: window.innerHeight,
            edit: false,
            add: false,
            del: false
        },
        tab: {
            "user": { name: "用户", icon: "user" },
            "rep": { name: "报表", icon: "data-analysis" },
            "auth": { name: "权限", icon: "lock" },
            "stat": { name: "统计", icon: "pie-chart" }
        },
        err: {
            "-1" : "错误：未选择数据",
            "-2" : "错误：编辑多条数据"
        }
    },
    computed: {
        // 高度
        tableH() {
            return this.setting.winH - 170
        },
        // 搜索
        search() {
            let rs = this.query ? this.tables[this.curTab].filter(
                r => Object.keys(r).map(
                    x => `${r[x]}`.indexOf(this.query) > -1
                ).includes(true)
            ) : this.tables[this.curTab]
            this.tblLen = rs.length
            return rs.slice((this.curPage - 1) * 100, this.curPage * 100)
        },
        // 用户
        userNames() {
            return this.tables.user.map(v => v.用户)
        },
        // 部门
        userDepts() {
            return new Set(this.tables.user.map(v => v.部门))
        },
        // 报表类别
        repTypes() {
            return new Set(this.tables.rep.map(v => v.报表类别))
        },
        // 报表名称
        repNames() {
            return this.tables.rep.map(v => v.报表名称)
        }
    },
    methods: {
        // 消息
        msg(msg, type) {
            const offset = 10
            const duration = 2000
            this.$message[type]({
                message: msg,
                offset: offset,
                duration: duration
            })
        },
        // 格式
        dateFormat() {
            let f = "YYYY/MM/DD HH:mm:ss"
            this.tables.stat.forEach(r => {
                r.本地时间 = dayjs(r.本地时间).format(f)
                r.服务器时间 = dayjs(r.服务器时间).format(f)
            })
        },
        // 清空
        clear() {
            this.curPage = 1
            this.sel = []
            this.$refs[this.curTab][0].clearSelection()
            Object.keys(this.form).forEach(k => this.form[k] = "")
        },
        // 标签
        tabClick(t) {
            if (this.curTab != t) {
                this.tblLen = this.tables[t].length
                this.curTab = t
                this.clear()
            }
        },
        // 弹出
        dial(op) {
            if (op == "add" || op == "edit") {
                this.dialTitle = `${(op == 'add')? '添加' : '编辑'}${this.tab[this.curTab].name}`
                if (op == "edit") {
                    if (this.validate(op) < 0) return
                    this.form = this.sel.at(-1)
                }
            }
            this.setting[op] = true
        },
        // 选择
        rowClick(row) { this.$refs[this.curTab][0].toggleRowSelection(row) },
        select(sel) { this.sel = sel },
        selectAll(sel) {
            if (!sel.length) this.sel = sel
            else this.sel = this.search
        },
        // 校验
        validate(op) {
            let e = 0;
            let num = this.sel.length
            switch (op) {
                case "edit":
                    if (num == 0) e = -1
                    else if (num > 1) e = -2
                    break
                case "del":
                    if (num == 0) e = -1
                    break
            }
            if (e < 0) this.msg(this.err[`${e}`], "error")
            return e
        },
        // 操作
        exec(op) {
            if (this.validate(op) < 0) return
            if (op == 'add' && this.curTab == 'auth') {
                this.form.报表名称 = this.form.报表名称.join()
            }
            axios.post(
                `/api/Entry/${op[0].toUpperCase()}${op.slice(1)}`,
                {
                    type: this.curTab,
                    data: (op == 'del') ? this.sel : this.form
                }
            ).then(
                res => {
                    let t = this.curTab
                    if (op == "copy") {
                        t = "auth"
                        op = "edit"
                    }
                    this.tables[t] = res.data
                    if (t == "stat") this.dateFormat()
                    this.setting[op] = false
                    this.msg("操作成功", "success")
                    this.clear()
                }
            ).catch(err => {
                this.msg(`错误：${err}`, "error")
                this.clear()
            })
        }
    },
    mounted() {
        // 获取数据
        axios.get("/api/Entry/GetAll").then(
            res => {
                this.tables = res.data
                this.tblLen = this.tables['user'].length
                this.dateFormat()
            }
        )
        // 监听窗口
        window.addEventListener("resize", () => this.setting.winH = window.innerHeight)
    }
});