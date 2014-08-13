function output = ANN_Think(input, ann)

for j = 1:length(ann.Layers{1}.Nodes)-1
    ann.Layers{1}.Nodes{j}.Value = input(j);
end
ann.Layers{1}.Nodes{end}.Value = 1; % bias

for i = 2:length(ann.Layers)
    for j = 1:length(ann.Layers{i}.Nodes)-1
        for k = 1:length(ann.Layers{i-1}.Nodes)
            addInput = ann.Layers{i-1}.Nodes{k}.Value * ann.Layers{i-1}.Nodes{k}.Weights(j);
            ann.Layers{i}.Nodes{j}.Value = ann.Layers{i}.Nodes{j}.Value + addInput;
        end
        ann.Layers{i}.Nodes{end}.Value = 1;
    end
end

output = 0;
for j = 1:length(ann.Layers{end}.Nodes)
    output = output + ann.Layers{end}.Nodes{j}.Weights(1) * ann.Layers{end}.Nodes{j}.Value;
end

end