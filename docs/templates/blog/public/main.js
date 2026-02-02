export default {
  start: async () => {
    const container = document.getElementById('blog-posts');
    if (!container) {
      return;
    }

    const postsRoot = container.dataset.postsRoot || '_posts';

    try {
      const [manifest, searchIndex] = await Promise.all([
        fetch(new URL('manifest.json', document.baseURI)).then((res) => res.json()),
        fetch(new URL('index.json', document.baseURI)).then((res) => res.json())
      ]);

      const posts = (manifest.files || [])
        .filter((file) => file.type === 'Conceptual' &&
          typeof file.source_relative_path === 'string' &&
          file.source_relative_path.startsWith(`${postsRoot}/`))
        .map((file) => buildPostEntry(file, searchIndex))
        .filter((post) => post !== null)
        .sort((a, b) => b.date.getTime() - a.date.getTime());

      if (posts.length === 0) {
        container.textContent = 'No posts yet.';
        return;
      }

      const list = document.createElement('div');
      list.className = 'blog-list';

      posts.forEach((post) => {
        const article = document.createElement('article');
        article.className = 'blog-entry';

        const title = document.createElement('h3');
        const link = document.createElement('a');
        link.href = post.href;
        link.textContent = post.title;
        title.appendChild(link);

        const meta = document.createElement('div');
        meta.className = 'blog-meta';
        meta.textContent = post.dateLabel;

        article.appendChild(title);
        article.appendChild(meta);

        if (post.summary) {
          const summary = document.createElement('p');
          summary.className = 'blog-summary';
          summary.textContent = post.summary;
          article.appendChild(summary);
        }

        list.appendChild(article);
      });

      container.replaceChildren(list);
    } catch (error) {
      container.textContent = 'Unable to load posts.';
      // eslint-disable-next-line no-console
      console.error('Failed to load blog posts', error);
    }
  }
};

function buildPostEntry(file, searchIndex) {
  const dateInfo = getDateFromSourcePath(file.source_relative_path);
  if (!dateInfo) {
    return null;
  }

  const href = file.output?.['.html']?.relative_path;
  if (!href) {
    return null;
  }

  const searchItem = searchIndex[href];
  const rawTitle = searchItem?.title || dateInfo.titleFallback;
  const title = rawTitle.split(' | ')[0].trim() || dateInfo.titleFallback;
  const summary = searchItem?.summary || '';

  return {
    href,
    title,
    summary,
    date: dateInfo.date,
    dateLabel: formatDate(dateInfo.date)
  };
}

function getDateFromSourcePath(path) {
  const fileName = path.split('/').pop() || '';
  const match = fileName.match(/^(\d{4})-(\d{2})-(\d{2})-(.+)\.md$/i);
  if (!match) {
    return null;
  }

  const [, year, month, day, slug] = match;
  const date = new Date(`${year}-${month}-${day}T00:00:00Z`);
  const titleFallback = slug
    .replace(/[-_]+/g, ' ')
    .replace(/\b\w/g, (letter) => letter.toUpperCase());

  return { date, titleFallback };
}

function formatDate(date) {
  return new Intl.DateTimeFormat(undefined, {
    year: 'numeric',
    month: 'long',
    day: '2-digit'
  }).format(date);
}
