// ====== 1. 5つの変数を管理するオブジェクトとUI ======
let controls = {
  v1: 0.5,
  v2: -0.3,
  v3: 0.8,
  v4: 0.2,
  v5: -0.9,
};
let sliders = {};

// パーティクルを管理する配列
let particles = [];
const numCopies = 5;

// ====== パーティクルを管理するクラス ======
class Particle {
  constructor(type, objectDrawer) {
    this.pos = createVector(random(-width / 2, width / 2), random(-height / 2, height / 2));
    this.vel = p5.Vector.random2D();
    this.vel.mult(random(1, 3));
    this.type = type;
    this.objectDrawer = objectDrawer;
  }

  update(direction) {
    let moveVel = this.vel.copy();
    moveVel.mult(direction);
    this.pos.add(moveVel);
    this.edges();
  }

  display() {
    this.objectDrawer(this.pos.x, this.pos.y);
  }

  edges() {
    if (this.pos.x > width / 2 || this.pos.x < -width / 2) {
      this.vel.x *= -1;
    }
    if (this.pos.y > height / 2 || this.pos.y < -height / 2) {
      this.vel.y *= -1;
    }
  }
}

// ====== 2. 変数を基にした3つの素材オブジェクト（変更なし）======
function drawObject1(x, y) {
  let weight = map(controls.v1, -1, 1, 1, 10);
  let size = map(controls.v2, -1, 1, 50, 150);
  let hue = map(controls.v3, -1, 1, 0, 120);
  let rotation = map(controls.v4, -1, 1, 0, TWO_PI);
  let sides = floor(map(controls.v5, -1, 1, 3, 10));

  push();
  translate(x, y);
  rotate(rotation);
  stroke(hue, 90, 90);
  strokeWeight(weight);
  noFill();
  
  beginShape();
  for (let i = 0; i < sides; i++) {
    let angle = map(i, 0, sides, 0, TWO_PI);
    let sx = cos(angle) * size;
    let sy = sin(angle) * size;
    vertex(sx, sy);
  }
  endShape(CLOSE);
  pop();
}

function drawObject2(x, y) {
  let numLines = floor(map(controls.v1, -1, 1, 4, 20));
  let minLength = map(controls.v2, -1, 1, 20, 50);
  let hue = map(controls.v3, -1, 1, 120, 240);
  let maxLength = map(controls.v4, -1, 1, 100, 200);
  let rotation = map(controls.v5, -1, 1, 0, PI);

  push();
  translate(x, y);
  rotate(rotation);
  stroke(hue, 80, 100);
  strokeWeight(2);

  for (let i = 0; i < numLines; i++) {
    let length = map(i, 0, numLines, minLength, maxLength);
    let angle = map(i, 0, numLines, 0, TWO_PI);
    line(0, 0, cos(angle) * length, sin(angle) * length);
  }
  pop();
}

function drawObject3(x, y) {
  let numCircles = floor(map(controls.v1, -1, 1, 5, 15));
  let clusterRange = map(controls.v2, -1, 1, 30, 120);
  let hue = map(controls.v3, -1, 1, 240, 360);
  let offset = map(controls.v4, -1, 1, -50, 50);
  let circleSize = map(controls.v5, -1, 1, 5, 30);

  push();
  translate(x, y);
  noStroke();
  
  for (let i = 0; i < numCircles; i++) {
    let angle = map(i, 0, numCircles, 0, TWO_PI);
    let radius = map(i, 0, numCircles, 0, clusterRange) + offset;
    let sx = cos(angle) * radius;
    let sy = sin(angle) * radius;
    
    let saturation = map(radius, 0, clusterRange, 100, 50);
    fill(hue, saturation, 90, 80);
    ellipse(sx, sy, circleSize, circleSize);
  }
  pop();
}


// ====== セットアップ処理 ======
function setup() {
  let canvas = createCanvas(windowWidth, windowHeight);
  // ▼▼▼【変更点】右クリックメニューを無効化 ▼▼▼
  canvas.elt.addEventListener('contextmenu', (e) => e.preventDefault());
  
  colorMode(HSB, 360, 100, 100, 100);

  // デバッグ用UI（スライダー）を作成
  let yPos = 20;
  for (let i = 1; i <= 5; i++) {
    let key = 'v' + i;
    sliders[key] = createSlider(-1, 1, controls[key], 0.01);
    sliders[key].position(20, yPos);
    sliders[key].addClass('p5-slider');
    yPos += 30;
  }
  
  // パーティクルを生成
  for (let i = 0; i < numCopies; i++) {
    particles.push(new Particle(1, drawObject1));
    particles.push(new Particle(2, drawObject2));
    particles.push(new Particle(3, drawObject3));
  }
}

// ====== UIを更新・描画するヘルパー関数（変更なし）======
function updateControls() {
  for (let i = 1; i <= 5; i++) {
    let key = 'v' + i;
    controls[key] = sliders[key].value();
  }
}

function drawGUI() {
  push();
  resetMatrix();
  fill(0, 150);
  noStroke();
  rect(10, 10, 170, 160, 10);
  fill(255);
  textSize(14);
  let yPos = 35;
  for (let i = 1; i <= 5; i++) {
    let key = 'v' + i;
    text(`v${i}: ${controls[key].toFixed(2)}`, 145, yPos - 5);
    yPos += 30;
  }
  pop();
}


// ====== メインの描画処理 ======
function draw() {
  background(0);
  
  updateControls();
  drawGUI();

  // ▼▼▼【変更点】左右のクリックに応じて進行方向を決定 ▼▼▼
  let direction = 0;
  if (mouseIsPressed) {
    if (mouseButton === LEFT) {
      direction = 1; // 左クリック：正方向
    } else if (mouseButton === RIGHT) {
      direction = -1; // 右クリック：逆方向
    }
  }

  // パーティクルの位置を更新
  for (let p of particles) {
    p.update(direction);
  }

  // ====== 万華鏡の演出（変更なし）======
  const numSegments = 6;
  const angle = TWO_PI / numSegments;
  translate(width / 2, height / 2);

  const drawScene = () => {
    for (let p of particles) {
      p.display();
    }
  };

  for (let i = 0; i < numSegments; i++) {
    push();
    rotate(angle * i);
    drawScene();
    push();
    scale(1, -1);
    drawScene();
    pop();
    pop();
  }
}

function windowResized() {
  resizeCanvas(windowWidth, windowHeight);
}