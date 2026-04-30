const internalMarker = "🔒";

function relativeRoot() {
  return document.querySelector('meta[name="docfx:rel"]')?.content || "";
}

function createLockIcon() {
  const image = document.createElement("img");
  image.className = "api-toc-lock";
  image.src = `${relativeRoot()}assets/api-icons/Lock.svg`;
  image.alt = "Internal";
  image.title = "Internal";
  image.loading = "lazy";
  return image;
}

function replaceTextMarker(element) {
  if (element.dataset.apiLockDecorated === "true" || !element.textContent.includes(internalMarker)) {
    return;
  }

  for (const node of element.childNodes) {
    if (node.nodeType === Node.TEXT_NODE && node.nodeValue.includes(internalMarker)) {
      node.nodeValue = node.nodeValue.replace(internalMarker, "").trimEnd();
      element.appendChild(createLockIcon());
      element.dataset.apiLockDecorated = "true";
      return;
    }
  }
}

function decorateTocLocks(root = document) {
  root.querySelectorAll(".toc a, #toc a, #affix a").forEach(replaceTextMarker);
}

document.addEventListener("DOMContentLoaded", () => {
  decorateTocLocks();

  const observer = new MutationObserver((records) => {
    for (const record of records) {
      for (const node of record.addedNodes) {
        if (node.nodeType === Node.ELEMENT_NODE) {
          decorateTocLocks(node);
        }
      }
    }
  });

  observer.observe(document.body, { childList: true, subtree: true });
});
