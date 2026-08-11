/* Sinh hai khối nhúng sẵn cho mẫu giấy báo: font dạng data URI và CSS Tailwind đã biên dịch.
   Chạy: npm run build (trong thư mục này). Ghi đè thẳng vào mẫu, giữa cặp thẻ đánh dấu. */

import { execFileSync } from 'node:child_process';
import { readFileSync, writeFileSync, rmSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const THU_MUC = dirname(fileURLToPath(import.meta.url));
const MAU = join(THU_MUC, '..', 'html', 'giay-bao-trung-tuyen.html');
const CSS_TAM = join(THU_MUC, 'tailwind-output.css');

/* Chỉ những họ và độ đậm mà mẫu thực sự dùng. Tải thừa một độ đậm là cộng thêm vài chục KB vào
   mỗi giấy báo, nhân với số thí sinh của cả mùa tuyển sinh. */
const FONT_URL = 'https://fonts.googleapis.com/css2'
    + '?family=Montserrat:ital,wght@0,400;0,600;0,700;1,400'
    + '&family=Playfair+Display:wght@700'
    + '&family=Share+Tech+Mono'
    + '&display=swap';

/* User-Agent của trình duyệt hiện đại để Google trả về woff2 - định dạng nhẹ nhất. */
const UA = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 '
    + '(KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36';

function thayKhoi(html, ten, noiDung) {
    const mo = `<!-- ${ten}:START -->`;
    const dong = `<!-- ${ten}:END -->`;
    const batDau = html.indexOf(mo);
    const ketThuc = html.indexOf(dong);
    if (batDau === -1 || ketThuc === -1) {
        throw new Error(`Không thấy cặp thẻ đánh dấu ${ten} trong mẫu.`);
    }
    return html.slice(0, batDau + mo.length) + '\n' + noiDung + '\n    ' + html.slice(ketThuc);
}

/* Google trả về cả cyrillic và greek. Giấy báo chỉ có tiếng Việt nên giữ đúng hai bảng mã cần dùng,
   bỏ phần còn lại: mỗi bảng mã thừa là một tệp woff2 nhúng vào từng giấy báo. */
const SUBSET_GIU = ['vietnamese', 'latin'];

function locSubset(css) {
    const khoi = css.split(/(?=\/\*\s*[a-z-]+\s*\*\/)/i);
    return khoi
        .filter(k => {
            const ten = k.match(/^\/\*\s*([a-z-]+)\s*\*\//i);
            return ten ? SUBSET_GIU.includes(ten[1].toLowerCase()) : true;
        })
        .join('');
}

async function nhungFont() {
    const goc = await (await fetch(FONT_URL, { headers: { 'User-Agent': UA } })).text();
    const css = locSubset(goc);
    const urls = [...new Set([...css.matchAll(/url\((https:\/\/[^)]+\.woff2)\)/g)].map(m => m[1]))];

    const tep = new Map();
    for (const url of urls) {
        const buf = Buffer.from(await (await fetch(url)).arrayBuffer());
        tep.set(url, `data:font/woff2;base64,${buf.toString('base64')}`);
    }

    let ketQua = css;
    for (const [url, data] of tep) {
        ketQua = ketQua.split(url).join(data);
    }

    const soByte = [...tep.values()].reduce((tong, d) => tong + d.length, 0);
    console.log(`font: ${tep.size} tệp woff2, ${(soByte / 1024).toFixed(0)} KB sau khi mã hoá base64`);
    return `    <style>\n${ketQua}\n    </style>`;
}

function bienDichTailwind() {
    execFileSync(process.execPath, [
        join(THU_MUC, 'node_modules', 'tailwindcss', 'lib', 'cli.js'),
        '--input', join(THU_MUC, 'tailwind-input.css'),
        '--output', CSS_TAM,
        '--config', join(THU_MUC, 'tailwind.config.js'),
        '--minify'
    ], { stdio: 'inherit' });

    const css = readFileSync(CSS_TAM, 'utf8');
    rmSync(CSS_TAM, { force: true });
    console.log(`tailwind: ${(css.length / 1024).toFixed(0)} KB sau khi rút gọn`);
    return `    <style>${css}</style>`;
}

/* Dấu đỏ và chữ ký tươi nhúng thẳng vào thẻ img: Gotenberg chỉ nhận đúng file index.html, ảnh để
   đường dẫn tương đối sẽ 404 và bản in ra chỉ còn chữ alt. Nguồn đọc từ data-asset nên chạy lại
   nhiều lần vẫn ra cùng kết quả. */
function nhungAnh(html) {
    return html.replace(/data-asset="([^"]+)"\s+src="[^"]*"/g, (_, ten) => {
        const buf = readFileSync(join(THU_MUC, '..', 'assets', ten));
        console.log(`ảnh ${ten}: ${(buf.length / 1024).toFixed(0)} KB`);
        return `data-asset="${ten}" src="data:image/png;base64,${buf.toString('base64')}"`;
    });
}

const khoiTailwind = bienDichTailwind();
const khoiFont = await nhungFont();

let html = readFileSync(MAU, 'utf8');
html = thayKhoi(html, 'FONT', khoiFont);
html = thayKhoi(html, 'TW', khoiTailwind);
html = nhungAnh(html);
writeFileSync(MAU, html, 'utf8');

console.log(`mẫu: ${(html.length / 1024).toFixed(0)} KB`);
