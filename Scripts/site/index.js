var app = new Vue({
    el: '#vue',
    data: {
        winH: window.innerHeight,
        repData: [],
        user: '',
    },
    computed: {
        mainHeight() { return { 'height': `${this.winH - 240}px` } },
        repTypes() { return new Set(this.repData.map(x => x.报表类别)) }
    },
    methods: {
        rep(t) { return this.repData.filter(x => x.报表类别 == t) },
        addStats(r) {
            axios.post(
                '/api/Entry/AddStat',
                {
                    '用户': this.user,
                    '本地时间': dayjs().format(),
                    '报表名称': r.报表名称,
                    '渠道': '点击'
                }
            )
        }
    },
    mounted() {
        // 获取用户信息及用户报表
        let loc = window.location
        let url = `${loc.hostname}${loc.port? `:${loc.port}` : ''}`
        let uuid = new URL(loc.href).searchParams.get('uuid')
        axios.get(
            `/api/Entry/GetUserReps?uuid=${uuid? uuid : '1'}&url=${url}`
        ).then(res => {
            this.user = res.data.user.用户
            res.data.rep.forEach(
                x => x.链接 = x.链接 ?
                    `${loc.protocol}//${res.data.href}/digiwin/kanban/v10/entry/access/${x.链接}`
                    + `?fine_username=admin&user=${this.user}` : `/home/report?name=${x.报表名称}&user=${this.user}`
            )
            this.$set(this,'repData',res.data.rep)
        })
    }
});