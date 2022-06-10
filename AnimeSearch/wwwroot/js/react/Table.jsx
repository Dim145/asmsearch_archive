const img_extensions = ["jpg", "jpeg", "png", "ico", "webp", "svg"];

/**
 * Class React représantant un tableau et qui peut être trier en cliquant sur les nom de colonnes.
 * 
 * Les propriétées attendus sont:
 *  - un champs cols qui contient un tableau de string. (la taille du tableau = nb colonnes)
 *  - un champs rows qui contient un tableau d'objet spécifique. La taille est illimité.
 *  
 * Les objets de chaques rows doivent correspondre à cela:
 *  - un attribut "datas" représantant les données de chaques ligne. C'est un tableau de string de la même taille que les colonnes.*
 *  - un attribut "key" qui est un identifiant unique de la ligne. Il est utilisé pour l'attribut key et id. (optionnel)
 *  - un attribut "className" qui contient les class sous forme de string. ex: "site bg-danger". (optionnel)
 *  
 * * => si un lien est dans les données, une balise "a" seras placé. Si ce lien donne sur une image, se seras la balise "img"
 *   => Il est possible de placer un element qui contient un attribut "href" et "text" pour avoir un text apparait différent du lien
 * */
class Table extends React.Component
{
    /**
     * construit l'objets et initialise les propriétés.
     * 
     * @param {any} props
     */
    constructor(props)
    {
        super(props);

        this.state = {
            rows: (this.props.rows || []).slice(),
            cols: this.props.cols || [],
            cols_state: [],
            img_height: this.props.imgHeight || 25
        };

        if (this.state.rows.length > 0 && this.state.rows[0].datas == undefined)
            this.state.rows = this.state.rows.map(r => { return { datas: r } });

        this.state.cols.map(c => this.state.cols_state.push(0));

        this.colClick = this.colClick.bind(this);
    }

    colClick(col_index)
    {
        const col_state  = this.state.cols_state[col_index];
        let cols_state = [];

        this.state.cols_state.map(c => cols_state.push(0));

        cols_state[col_index] = col_state == 1 ? -1 : col_state == 0 ? 1 : 0; // valeur -1 / 0 / 1

        let ordered_rows = this.state.rows.sort((a, b) =>
        {
            const v1 = typeof a.datas[col_index] === "string" ? !isNaN(a.datas[col_index]) ? parseInt(a.datas[col_index]) : this.isDate(a.datas[col_index]) ? new Date(a.datas[col_index]) : a.datas[col_index].toLowerCase() : a.datas[col_index];
            const v2 = typeof b.datas[col_index] === "string" ? !isNaN(b.datas[col_index]) ? parseInt(b.datas[col_index]) : this.isDate(b.datas[col_index]) ? new Date(b.datas[col_index]) : b.datas[col_index].toLowerCase() : b.datas[col_index];
            
            return v1 != v2 ? v1 < v2 ? -1 : 1 : 0;
        });

        if (cols_state[col_index] == -1)
            ordered_rows = ordered_rows.reverse();

        if (cols_state.reduce((a, b) => a + b) == 0) // l'ordre original depend de la bdd
        {
            ordered_rows = (this.props.rows || []).slice();

            if (ordered_rows.length > 0 && ordered_rows[0].datas == undefined)
                ordered_rows = ordered_rows.map(r => { return { datas: r } });
        }

        this.setState({ rows: ordered_rows, cols_state: cols_state });
    }

    render()
    {
        return <div>
            <table className="table table-dark table-striped table-hover">
                <thead>
                    <tr key="header">
                        {this.state.cols.map((c, i) => <th className="text-center" key={"th-" + i}>
                            <a className="link-white" onClick={() => this.colClick(i)}>
                                <div className="row m-0 align-items-center ">
                                    <div className={"col p-0" + (this.state.cols_state[i] != 0 ? " text-right" : "")}>{c}</div> {this.state.cols_state[i] != 0 ?
                                    <div className="col p-0 text-left">{this.state.cols_state[i] == 1 ? <span>&#9662;</span> : <span>&#9652;</span>}</div> : ""}
                                </div>
                            </a>
                        </th>)}
                    </tr>
                </thead>

                <tbody>
                    {this.state.rows.map((r, i) =>
                    {
                        const id = r.key != undefined ? r.key : i;

                        return <tr key={id} id={id != i ? id : ""} className={r.className != undefined ? r.className : ""}> {r.datas.map((c, i2) =>
                            <td key={"td-" + i + "-" + i2}>
                                {
                                    //Dans le cas ou le text est une string de type lien
                                    typeof c === 'string' && c.startsWith("http") ? <a className="link-white" target="_blank" href={c}>{this.isUrlImage(c) ? <img height={this.state.img_height} src={c} /> : c}</a> :
                                    // Dans le cas ou c'est un objet avec un attribut href et text
                                    c != undefined && c["href"] != undefined && c["text"] != undefined ? <a className="link-white" target="_blank" href={c["href"]}>{c["text"]}</a> : c["href"] != undefined ? "" :
                                    // Dans les autres cas
                                    c
                                }
                            </td>
                        )}
                        </tr>
                    })}
                </tbody>
            </table>
        </div>
    }

    isDate(date)
    {
        const d = new Date(Date.parse(date));

        return (d !== "Invalid Date") && !isNaN(d);
    }

    isUrlImage(url)
    {
        return img_extensions.filter(ext => url.endsWith(ext)).length > 0;
    }
}